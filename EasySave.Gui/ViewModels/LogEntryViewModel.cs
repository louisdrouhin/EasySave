namespace EasySave.Gui.ViewModels;

public class LogEntryViewModel : ViewModelBase
{
    public string LogText { get; }

    public LogEntryViewModel(string text)
    {
        LogText = text;
    }
}
