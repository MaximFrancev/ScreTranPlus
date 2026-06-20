using System.Windows;
using System.Windows.Input;

namespace ScreTranPlus;

/// <summary>
/// Логика взаимодействия для SelectionWindow.xaml
/// </summary>
public partial class SelectionWindow : Window
{
    public SelectionWindow()
    {
        InitializeComponent();

        this.DataContext = App.GetService<SelectionViewModel>();
        Loaded += Window_Loaded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var windowService = App.GetService<IWindowService>();

        // Находим ViewModel этого окна, чтобы узнать его порядковый номер
        var vm = DataContext as SelectionViewModel;

        // Назначаем главным владельцем ТОЛЬКО самую первую рамку (с индексом 0)
        if (vm != null && vm.WindowIndex == 0)
        {
            windowService.SetOwner(this);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}