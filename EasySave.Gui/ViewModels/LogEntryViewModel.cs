using System.Text.Json;

namespace EasySave.Gui.ViewModels;

public class LogEntryViewModel : ViewModelBase
{
    public string LogText { get; }

    public LogEntryViewModel(string text)
    {
        try
        {
            // Parse and pretty-print the JSON with indentation
            using (var doc = JsonDocument.Parse(text))
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
                // Add visual separator at the end
                LogText = formattedJson + "\n" + new string('─', 80);
            }
        }
        catch
        {
            // If JSON parsing fails, display raw text with separator
            LogText = text + "\n" + new string('─', 80);
        }
    }
}
