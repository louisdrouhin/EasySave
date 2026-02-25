using System.Text.Json;

namespace EasyLog.Lib;

// Formateur pour les logs en JSON
// Sérialise les entrées de log au format JSON
public class JsonLogFormatter : ILogFormatter
{
    // Formate une entrée de log en JSON
    // @param timestamp - date/heure de l'entrée
    // @param name - nom du backup
    // @param content - contenu de l'entrée
    // @returns chaîne JSON minifiée contenant l'entrée
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

    // Ferme le fichier JSON en ajoutant les marqueurs de fin
    // @param filePath - chemin du fichier JSON à fermer
    public void Close(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                // Ajoute les marqueurs de fin du tableau JSON si absent
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
