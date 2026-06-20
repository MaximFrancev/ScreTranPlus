using System.Drawing;
using System.Windows;

namespace ScreTranPlus;

public interface IWindowService
{
    Rectangle? GetWindowCoordinates(string windowName);

    void CloseAll();

    void Close(string windowName, string? instanceId = null);

    void Register<T>() where T : Window;

    void Show(string windowName, string? instanceId = null);

    void SetWindowClickThru(string windowName, string? instanceId = null);

    void SetWindowClickable(string windowName, string? instanceId = null);

    void ExcludeFromCapture(string windowName, string? instanceId = null);

    void SetOwner(Window owner);
}