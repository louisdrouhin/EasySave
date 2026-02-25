using System.Text;
using System.Xml;

namespace EasyLog.Lib;

// Formateur pour les logs en XML
// Sérialise les entrées de log au format XML
public class XmlLogFormatter : ILogFormatter
{
    // Formate une entrée de log en XML
    // Crée un élément logEntry avec timestamp, name et contenu
    // @param timestamp - date/heure de l'entrée
    // @param name - nom du backup
    // @param content - contenu de l'entrée (convertis en éléments XML)
    // @returns chaîne XML minifiée contenant l'entrée
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
            // Convertit chaque propriété en élément XML
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

    // Ferme le fichier XML en ajoutant le marqueur de fin
    // @param filePath - chemin du fichier XML à fermer
    public void Close(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                // Ajoute le marqueur de fin de l'élément logs si absent
                if (!content.EndsWith("</logs>"))
                {
                    File.AppendAllText(filePath, "</logs>");
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error while closing the XML log file : {filePath}",
                ex);
        }
    }

    // Convertit une chaîne en un nom d'élément XML valide
    // @param name - nom à convertir
    // @returns nom d'élément XML valide
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
