using System.Text.Json;

namespace EasyLog.Lib;

// Formatter for JSON logs
// Serializes log entries to JSON format
public class JsonLogFormatter : ILogFormatter
{
    // Formats a log entry to JSON
    // @param timestamp - date/time of the entry
    // @param name - name of the backup
    // @param content - entry content
    // @returns minified JSON string containing the entry
    public string Format(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var logEntry = new
        {
            timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            name = name,
            content = content
        };

        return JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
    }

    // Closes the JSON file by adding end markers
    // @param filePath - path to the JSON file to close
    public void Close(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                // Adds the JSON array end markers if not present
                if (!content.EndsWith("]}"))
                {
                    File.AppendAllText(filePath, "]}");
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error while closing the JSON log file: {filePath}",
                ex);
        }
    }
}
