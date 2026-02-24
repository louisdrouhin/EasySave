using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

public class ExtensionItemViewModel : ViewModelBase
{
    public string DisplayText { get; }
    public ICommand RemoveCommand { get; }

    public ExtensionItemViewModel(string ext, Action<ExtensionItemViewModel> removeCallback)
    {
        DisplayText = ext;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
