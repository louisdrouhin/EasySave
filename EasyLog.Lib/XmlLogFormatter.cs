using System.Text;
using System.Xml;

namespace EasyLog.Lib;

// Formatter for XML logs
// Serializes log entries to XML format
public class XmlLogFormatter : ILogFormatter
{
    // Formats a log entry to XML
    // Creates a logEntry element with timestamp, name, and content
    // @param timestamp - date/time of the entry
    // @param name - name of the backup
    // @param content - entry content (converted to XML elements)
    // @returns minified XML string containing the entry
    public string Format(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false,
            ConformanceLevel = ConformanceLevel.Fragment
        }))
        {
            writer.WriteStartElement("logEntry");

            writer.WriteElementString("timestamp", timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteElementString("name", name);

            writer.WriteStartElement("content");
            // Converts each property to an XML element
            foreach (var kvp in content)
            {
                writer.WriteStartElement(SanitizeXmlElementName(kvp.Key));
                writer.WriteString(kvp.Value?.ToString() ?? string.Empty);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        return sb.ToString();
    }

    // Closes the XML file by adding the end marker
    // @param filePath - path to the XML file to close
    public void Close(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                // Adds the logs element end marker if not present
                if (!content.EndsWith("</logs>"))
                {
                    File.AppendAllText(filePath, "</logs>");
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error while closing the XML log file: {filePath}",
                ex);
        }
    }

    // Converts a string to a valid XML element name
    // @param name - name to convert
    // @returns valid XML element name
    private string SanitizeXmlElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "item";

        var sanitized = new StringBuilder();

        if (!char.IsLetter(name[0]) && name[0] != '_')
            sanitized.Append('_');

        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                sanitized.Append(c);
            else
                sanitized.Append('_');
        }

        return sanitized.ToString();
    }
}
