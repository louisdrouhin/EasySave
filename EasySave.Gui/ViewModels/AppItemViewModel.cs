using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

public class AppItemViewModel : ViewModelBase
{
    public string DisplayText { get; }
    public ICommand RemoveCommand { get; }

    public AppItemViewModel(string appName, Action<AppItemViewModel> removeCallback)
    {
        DisplayText = appName;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
