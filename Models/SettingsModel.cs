using CommunityToolkit.Mvvm.ComponentModel;
using RawInput;
using System.Collections.ObjectModel;

namespace ScreTranPlus;

public partial class SettingsModel : ObservableObject
{
    [ObservableProperty]
    private int _fontSize;

    // Изменили тип с интерфейса IKey на конкретный класс Key для бесконфликтной десериализации
    [ObservableProperty]
    private Key _key;

    [ObservableProperty]
    private float _period;

    [ObservableProperty]
    private int _hideInterval;

    [ObservableProperty]
    private Enumerations.Translator _translator;

    [ObservableProperty]
    private Enumerations.Model _ocrModel;

    [ObservableProperty]
    private string _geminiApiKey;

    // Настройки цветового фильтра
    [ObservableProperty]
    private bool _isColorFilterEnabled;

    [ObservableProperty]
    private string _targetColorHex;

    [ObservableProperty]
    private int _colorTolerance;

    [ObservableProperty]
    private int _minColorPercentage;

    // Настройка целевого языка перевода
    [ObservableProperty]
    private Enumerations.TargetLanguage _targetLanguage;

    // Ручной режим и Локальный пре-фильтр
    [ObservableProperty]
    private bool _isManualModeEnabled;

    [ObservableProperty]
    private bool _isOcrPreFilterEnabled;

    [ObservableProperty]
    private Enumerations.Model _preFilterLanguage;

    [ObservableProperty]
    private ObservableCollection<WindowPositionModel> _selectionWindowPositions;

    [ObservableProperty]
    private WindowPositionModel _translationWindowPosition;

    public SettingsModel()
    {
        FontSize = 21;
        Key = new Key(0x7B);
        Period = 1.0f;
        HideInterval = 2;
        Translator = Enumerations.Translator.Google;
        OcrModel = Enumerations.Model.English;
        GeminiApiKey = string.Empty;

        IsColorFilterEnabled = false;
        TargetColorHex = "#000000";
        ColorTolerance = 25;
        MinColorPercentage = 30;

        TargetLanguage = Enumerations.TargetLanguage.Russian;

        IsManualModeEnabled = false;
        IsOcrPreFilterEnabled = true;
        PreFilterLanguage = Enumerations.Model.English;

        SelectionWindowPositions = new ObservableCollection<WindowPositionModel>();
        TranslationWindowPosition = new WindowPositionModel();
    }

    public void ResetWindowPositions()
    {
        foreach (var pos in SelectionWindowPositions)
        {
            pos.Left = 0;
            pos.Top = 0;
        }
        TranslationWindowPosition.Left = 0;
        TranslationWindowPosition.Top = 0;
    }

    public void ResetToDefault()
    {
        FontSize = 21;
        Key = new Key(0x7B);
        Period = 1.0f;
        HideInterval = 2;
        Translator = Enumerations.Translator.Google;
        OcrModel = Enumerations.Model.English;
        GeminiApiKey = string.Empty;

        IsColorFilterEnabled = false;
        TargetColorHex = "#000000";
        ColorTolerance = 25;
        MinColorPercentage = 30;

        TargetLanguage = Enumerations.TargetLanguage.Russian;

        IsManualModeEnabled = false;
        IsOcrPreFilterEnabled = true;
        PreFilterLanguage = Enumerations.Model.English;

        SelectionWindowPositions.Clear();
        SelectionWindowPositions.Add(new WindowPositionModel());
    }
}