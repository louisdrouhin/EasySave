using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel for an extension item in the lists
// Represents a file extension (e.g., .docx, .xlsx)
public class ExtensionItemViewModel : ViewModelBase
{
    // Text to display for the extension (e.g., ".docx")
    public string DisplayText { get; }

    // Command to remove this extension from the list
    public ICommand RemoveCommand { get; }

    // Creates a ViewModel for an extension
    // @param ext - extension to display
    // @param removeCallback - callback called when user removes the extension
    public ExtensionItemViewModel(string ext, Action<ExtensionItemViewModel> removeCallback)
    {
        DisplayText = ext;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
