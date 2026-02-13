using System.Text.Json;

namespace EasyLog.Lib;

public class JsonLogFormatter : ILogFormatter
{
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

    public void Close(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                if (!content.EndsWith("]}"))
                {
                    File.AppendAllText(filePath, "]}");
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error while closing the JSON log file : {filePath}",
                ex);
        }
    }
}
