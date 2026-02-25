using System.Text.Json;

namespace EasySave.Gui.ViewModels;

// ViewModel pour une entrée de log individuelle
// Formate et affiche l'entrée de log (JSON ou texte brut)
public class LogEntryViewModel : ViewModelBase
{
    // Texte de l'entrée formatée pour l'affichage
    public string LogText { get; }

    // Crée un ViewModel pour une entrée de log
    // Parse le JSON pour le formater de manière lisible
    // @param text - texte JSON brut de l'entrée de log
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
