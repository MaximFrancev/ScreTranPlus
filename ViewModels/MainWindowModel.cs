using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RawInput;
using System.Collections.ObjectModel;

namespace ScreTranPlus;

public partial class MainWindowModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly IExecutionService _executionService;
    private readonly IInputService _inputSevice;

    [ObservableProperty]
    private IParametersService _parameters;

    [ObservableProperty]
    private bool _isKeySetting;

    [ObservableProperty]
    private SettingsModel _settings;

    [ObservableProperty]
    private ObservableCollection<Enumerations.Translator> _translators;

    [ObservableProperty]
    private ObservableCollection<Enumerations.Model> _models;

    [ObservableProperty]
    private ObservableCollection<Enumerations.TargetLanguage> _targetLanguages;

    [RelayCommand]
    private void Start()
    {
        _windowService.SetWindowClickThru("TranslationWindow");
        for (int i = 0; i < Settings.SelectionWindowPositions.Count; i++)
        {
            _windowService.SetWindowClickThru("SelectionWindow", i.ToString());
        }
        _executionService.Start();
        Parameters.IsStarted = true;
    }

    [RelayCommand]
    private void Stop()
    {
        _windowService.SetWindowClickable("TranslationWindow");
        for (int i = 0; i < Settings.SelectionWindowPositions.Count; i++)
        {
            _windowService.SetWindowClickable("SelectionWindow", i.ToString());
        }
        _executionService.Stop();
        Parameters.IsStarted = false;
    }

    [RelayCommand]
    private void Close()
    {
        _settingsService.Save(Settings);
        _windowService.CloseAll();
    }

    [RelayCommand]
    private void ClearKey()
    {
        Settings.Key = new Key();
    }

    [RelayCommand]
    private void SetKey()
    {
        IsKeySetting = true;
    }

    [RelayCommand]
    private void ResetWindowsPosition()
    {
        Settings.ResetWindowPositions();
    }

    [RelayCommand]
    private void ResetSettingsToDefault()
    {
        Settings.ResetToDefault();
    }

    [RelayCommand]
    private void AddSelectionWindow()
    {
        var newPosition = new WindowPositionModel();
        Settings.SelectionWindowPositions.Add(newPosition);

        int index = Settings.SelectionWindowPositions.Count - 1;
        _windowService.Show("SelectionWindow", index.ToString());
        _windowService.ExcludeFromCapture("SelectionWindow", index.ToString());

        if (Parameters.IsStarted)
        {
            _windowService.SetWindowClickThru("SelectionWindow", index.ToString());
        }
    }

    [RelayCommand]
    private void RemoveSelectionWindow()
    {
        if (Settings.SelectionWindowPositions.Count <= 1)
            return;

        int lastIndex = Settings.SelectionWindowPositions.Count - 1;
        _windowService.Close("SelectionWindow", lastIndex.ToString());
        Settings.SelectionWindowPositions.RemoveAt(lastIndex);
    }

    public MainWindowModel(ISettingsService settingsService, IParametersService parametersService, IWindowService windowService, IExecutionService executionService, IInputService inputService)
    {
        _settingsService = settingsService;
        Parameters = parametersService;
        Settings = _settingsService.Settings;

        _translators =
        [
            Enumerations.Translator.Google,
            Enumerations.Translator.Yandex,
            Enumerations.Translator.Bing,
            Enumerations.Translator.Gemini,
        ];

        _models =
        [
            Enumerations.Model.English,
            Enumerations.Model.Korean,
            Enumerations.Model.Chinese,
            Enumerations.Model.Japanese,
            Enumerations.Model.GeminiVision,
        ];

        _targetLanguages =
        [
            Enumerations.TargetLanguage.Russian,
            Enumerations.TargetLanguage.Ukrainian,
            Enumerations.TargetLanguage.English,
            Enumerations.TargetLanguage.Spanish,
            Enumerations.TargetLanguage.German,
            Enumerations.TargetLanguage.French,
            Enumerations.TargetLanguage.Japanese
        ];

        IsKeySetting = false;

        _executionService = executionService;
        _windowService = windowService;

        for (int i = 0; i < Settings.SelectionWindowPositions.Count; i++)
        {
            _windowService.Show("SelectionWindow", i.ToString());
            _windowService.ExcludeFromCapture("SelectionWindow", i.ToString());
        }

        _windowService.Show("TranslationWindow");
        _windowService.ExcludeFromCapture("TranslationWindow");

        _inputSevice = inputService;
        _inputSevice.KeyDown += InputSevice_KeyDown;
    }

    private void InputSevice_KeyDown(HookEventArgs e)
    {
        if (IsKeySetting)
        {
            if (e.Key.Code != KeyCodes.Esc)
                Settings.Key = (Key)e.Key; // Добавили явное приведение типов (Cast) к классу Key

            IsKeySetting = false;
            return;
        }

        if (Equals(e.Key, Settings.Key))
        {
            if (Settings.IsManualModeEnabled)
            {
                if (Parameters.IsStarted)
                {
                    _executionService.TriggerManualTranslation();
                }
            }
            else
            {
                if (Parameters.IsStarted)
                {
                    Stop();
                }
                else
                {
                    Start();
                }
            }
        }
    }
}