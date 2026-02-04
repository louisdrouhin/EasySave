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
}
