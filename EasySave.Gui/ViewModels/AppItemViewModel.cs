using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel for a business application in the list
// Represents an application to monitor (e.g., CryptoSoft)
public class AppItemViewModel : ViewModelBase
{
    // Text to display for the application (e.g., "CryptoSoft")
    public string DisplayText { get; }

    // Command to remove this application from the list
    public ICommand RemoveCommand { get; }

    // Creates a ViewModel for a business application
    // @param appName - name of the application to display
    // @param removeCallback - callback called when user removes the application
    public AppItemViewModel(string appName, Action<AppItemViewModel> removeCallback)
    {
        DisplayText = appName;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
