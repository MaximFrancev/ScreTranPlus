using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace ScreTranPlus;

public class ExecutionService : IExecutionService
{
    private readonly IParametersService _parameters;
    private readonly ITranslationService _translationService;
    private readonly IWindowService _windowService;
    private readonly SettingsModel _settings;
    private Timer? _timer;
    private readonly string _regExPattern;

    private PaddleOcrAll? _paddleOcrAll;

    private Task? _lastTask;
    private string _lastLine;
    private float _timeWithoutUpdate;

    private Enumerations.Model _currentModel;

    private List<byte[]> _lastCapturedImages = new();

    public ExecutionService(ISettingsService settingsService, IParametersService parametersService, ITranslationService translationService, IWindowService windowService)
    {
        _parameters = parametersService;
        _windowService = windowService;
        _settings = settingsService.Settings;

        _lastLine = string.Empty;
        _translationService = translationService;

        _regExPattern = @"\W";
    }

    public void Start()
    {
        _timeWithoutUpdate = 0;
        _parameters.TranslatedLine = string.Empty;
        _lastLine = string.Empty;
        _lastCapturedImages.Clear();

        // Инициализируем локальный OCR если нужен обычный режим ИЛИ пре-фильтр для Gemini Vision
        if (_settings.OcrModel != Enumerations.Model.GeminiVision || _settings.IsOcrPreFilterEnabled)
        {
            InitializePaddleOcr();
        }

        // Запускаем таймер автоматического захвата только если НЕ включен ручной режим
        if (!_settings.IsManualModeEnabled)
        {
            var period = (int)(1000 * _settings.Period);
            _timer = new Timer(ProccessByTimerCommands, null, 0, period);
        }
    }

    public void Stop()
    {
        _timer?.Dispose();
    }

    public void TriggerManualTranslation()
    {
        // Запуск одиночного перевода в фоновом потоке (для Manual Mode)
        Task.Run(RecognizeTextAndTranslate);
    }

    private FullOcrModel GetOcrModel(Enumerations.Model model)
    {
        if (model == Enumerations.Model.English) return LocalFullModels.EnglishV4;
        if (model == Enumerations.Model.Japanese) return LocalFullModels.JapanV4;
        if (model == Enumerations.Model.Korean) return LocalFullModels.KoreanV4;
        if (model == Enumerations.Model.Chinese) return LocalFullModels.ChineseV4;
        return LocalFullModels.EnglishV4;
    }

    private void InitializePaddleOcr()
    {
        // Определяем, какой язык загружать в память
        var targetModel = _settings.OcrModel;
        if (targetModel == Enumerations.Model.GeminiVision)
        {
            targetModel = _settings.PreFilterLanguage; // Для Gemini Vision используем язык пре-фильтра
        }

        if (_paddleOcrAll != null && _currentModel == targetModel)
            return; // Модель уже загружена в память

        _currentModel = targetModel;

        _paddleOcrAll = new PaddleOcrAll(GetOcrModel(_currentModel), PaddleDevice.Onnx(2))
        {
            AllowRotateDetection = false,
            Enable180Classification = false,
        };
    }

    private void ProccessByTimerCommands(object? state)
    {
        if (_lastTask?.Status == TaskStatus.Running)
            return;

        _lastTask = Task.Run(RecognizeTextAndTranslate);
    }

    private string PaddleOCRRecognize(byte[] sampleImageData)
    {
        if (_paddleOcrAll == null) return string.Empty;

        using var src = Cv2.ImDecode(sampleImageData, ImreadModes.Color);
        if (src.Empty())
            return string.Empty;

        Cv2.Resize(src, src, new OpenCvSharp.Size(src.Width * 0.6, src.Height * 0.6));
        return _paddleOcrAll.Run(src).Text;
    }

    private bool IsDialogueBoxPresent(byte[] imgData)
    {
        if (!_settings.IsColorFilterEnabled)
            return true;

        try
        {
            using var src = Cv2.ImDecode(imgData, ImreadModes.Color);
            if (src.Empty()) return false;

            var hex = _settings.TargetColorHex.TrimStart('#');
            if (hex.Length != 6) return true;

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            int tol = _settings.ColorTolerance;

            var lower = new Scalar(Math.Max(0, b - tol), Math.Max(0, g - tol), Math.Max(0, r - tol));
            var upper = new Scalar(Math.Min(255, b + tol), Math.Min(255, g + tol), Math.Min(255, r + tol));

            using var mask = new Mat();
            Cv2.InRange(src, lower, upper, mask);

            int matchedPixels = Cv2.CountNonZero(mask);
            double totalPixels = src.Width * src.Height;
            double percentage = (matchedPixels / totalPixels) * 100;

            return percentage >= _settings.MinColorPercentage;
        }
        catch
        {
            return true;
        }
    }

    private bool HasSingleImageChanged(byte[] lastImg, byte[] currentImg, double changeThreshold = 0.02)
    {
        try
        {
            using var msLast = new MemoryStream(lastImg);
            using var msCurrent = new MemoryStream(currentImg);
            using var bmpLast = new Bitmap(msLast);
            using var bmpCurrent = new Bitmap(msCurrent);

            using var smallLast = new Bitmap(bmpLast, new System.Drawing.Size(16, 16));
            using var smallCurrent = new Bitmap(bmpCurrent, new System.Drawing.Size(16, 16));

            int diffPixels = 0;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var c1 = smallLast.GetPixel(x, y);
                    var c2 = smallCurrent.GetPixel(x, y);

                    int brightnessDiff = Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B);
                    if (brightnessDiff > 30)
                    {
                        diffPixels++;
                    }
                }
            }

            double diffPercentage = (double)diffPixels / 256;
            return diffPercentage >= changeThreshold;
        }
        catch
        {
            return true;
        }
    }

    private bool HaveImagesChanged(List<byte[]> currentImages)
    {
        if (_lastCapturedImages == null || _lastCapturedImages.Count != currentImages.Count)
        {
            _lastCapturedImages = currentImages;
            return true;
        }

        for (int i = 0; i < currentImages.Count; i++)
        {
            if (HasSingleImageChanged(_lastCapturedImages[i], currentImages[i]))
            {
                _lastCapturedImages = currentImages;
                return true;
            }
        }

        return false;
    }

    private List<byte[]> GetCapturedImages()
    {
        var images = new List<byte[]>();

        for (int i = 0; i < _settings.SelectionWindowPositions.Count; i++)
        {
            var coordinates = _windowService.GetWindowCoordinates($"SelectionWindow_{i}");
            if (coordinates == null)
                continue;

            using var bitmap = new Bitmap(coordinates.Value.Width, coordinates.Value.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(coordinates.Value.Left,
                                 coordinates.Value.Top,
                                 0,
                                 0,
                                 bitmap.Size,
                                 CopyPixelOperation.SourceCopy);
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            images.Add(stream.ToArray());
        }

        return images;
    }

    private void RecognizeTextAndTranslate()
    {
        try
        {
            var currentImages = GetCapturedImages();
            if (currentImages.Count == 0)
                return;

            // 1. ЦВЕТОВОЙ ФИЛЬТР
            bool isDialogueActive = false;
            foreach (var img in currentImages)
            {
                if (IsDialogueBoxPresent(img))
                {
                    isDialogueActive = true;
                    break;
                }
            }
            if (!isDialogueActive) return;

            // 2. СРАВНЕНИЕ КАДРОВ (только для автоматического режима)
            if (!_settings.IsManualModeEnabled && !HaveImagesChanged(currentImages))
            {
                return;
            }

            string translatedResult = string.Empty;

            if (_settings.OcrModel == Enumerations.Model.GeminiVision)
            {
                // 3. ЛОКАЛЬНЫЙ OCR ПРЕ-ФИЛЬТР (для экономии Gemini Vision лимитов)
                if (_settings.IsOcrPreFilterEnabled)
                {
                    bool textDetectedLocally = false;
                    foreach (var imgData in currentImages)
                    {
                        var localOcrText = PaddleOCRRecognize(imgData);
                        if (!string.IsNullOrWhiteSpace(localOcrText))
                        {
                            textDetectedLocally = true;
                            break;
                        }
                    }

                    // Если локальный PaddleOCR не нашел букв в рамках — отменяем отправку скриншотов в Gemini
                    if (!textDetectedLocally)
                    {
                        return;
                    }
                }

                // Шлем картинки напрямую в Gemini Vision
                translatedResult = _translationService.TranslateVision(currentImages, _settings.TargetLanguage);

                if (translatedResult == "PRESERVE_LAST")
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(translatedResult))
                {
                    _timeWithoutUpdate++;
                    if (_timeWithoutUpdate > _settings.HideInterval)
                    {
                        _timeWithoutUpdate = 0;
                        _parameters.TranslatedLine = string.Empty;
                        _lastLine = string.Empty;
                    }
                    return;
                }

                _timeWithoutUpdate = 0;
            }
            else
            {
                // Обычный режим
                var recognizedTexts = new List<string>();
                foreach (var imgData in currentImages)
                {
                    string text = PaddleOCRRecognize(imgData);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        recognizedTexts.Add(text.Trim());
                    }
                }

                var line = string.Join(" ", recognizedTexts);

                if (string.IsNullOrWhiteSpace(line))
                {
                    _timeWithoutUpdate++;
                    if (_timeWithoutUpdate > _settings.HideInterval)
                    {
                        _timeWithoutUpdate = 0;
                        _parameters.TranslatedLine = string.Empty;
                        _lastLine = string.Empty;
                    }
                    return;
                }

                _timeWithoutUpdate = 0;

                line = line.Trim()
                     .Replace("\n", " ")
                     .Replace(",.", ",")
                     .Replace(".,", ".")
                     .Replace("?.", "?")
                     .Replace(".?", "?")
                     .Replace("!.", "!")
                     .Replace(".!", "!")
                     .Replace("..", ".")
                     .Replace("..", "...");

                var nakedLine = Regex.Replace(line.ToLower(), _regExPattern, string.Empty);
                if (!_settings.IsManualModeEnabled && nakedLine == _lastLine)
                    return;

                _lastLine = nakedLine;

                translatedResult = _translationService.Translate(line, _settings.Translator, _settings.TargetLanguage);

                if (translatedResult == "PRESERVE_LAST")
                {
                    return;
                }
            }

            _parameters.TranslatedLine = translatedResult;
        }
        catch (Exception ex)
        {
            _parameters.TranslatedLine = $"Error: {ex.Message}";
        }
    }
}