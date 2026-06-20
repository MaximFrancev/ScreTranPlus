using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreTranPlus;

public partial class SelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private WindowPositionModel _position;

    [ObservableProperty]
    private IParametersService _parameters;

    // Хранит порядковый номер текущего окна (0, 1, 2 и т.д.)
    public int WindowIndex { get; private set; }

    public SelectionViewModel(ISettingsService settingsService, IParametersService parametersService)
    {
        Parameters = parametersService;
        // По умолчанию привязываемся к первому элементу в списке позиций
        Position = settingsService.Settings.SelectionWindowPositions[0];
        WindowIndex = 0;
    }

    // Метод, который мы вызовем при создании окна, чтобы настроить нужный индекс
    public void SetWindowIndex(int index, SettingsModel settings)
    {
        WindowIndex = index;
        if (index < settings.SelectionWindowPositions.Count)
        {
            Position = settings.SelectionWindowPositions[index];
        }
    }
}