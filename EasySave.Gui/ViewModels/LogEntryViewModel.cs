using System.Text.Json;

namespace EasySave.Gui.ViewModels;

// ViewModel for an individual log entry
// Formats and displays the log entry (JSON or plain text)
public class LogEntryViewModel : ViewModelBase
{
    // Formatted text of the entry for display
    public string LogText { get; }

    // Creates a ViewModel for a log entry
    // Parses JSON to format it in a readable way
    // @param text - raw JSON text of the log entry
    public LogEntryViewModel(string text)
    {
        try
        {
            using (var doc = JsonDocument.Parse(text))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
                LogText = formattedJson + "\n" + new string('─', 80);
            }
        }
        catch
        {
            LogText = text + "\n" + new string('─', 80);
        }
    }
}
