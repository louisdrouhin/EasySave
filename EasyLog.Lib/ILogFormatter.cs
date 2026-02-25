namespace EasyLog.Lib;

// Interface pour les formateurs de logs
// Implémentée par JsonLogFormatter et XmlLogFormatter
public interface ILogFormatter
{
    // Formate une entrée de log
    // @param timestamp - date/heure de l'entrée
    // @param name - nom du backup
    // @param content - contenu de l'entrée (dictionnaire de propriétés)
    // @returns chaîne formatée (JSON ou XML)
    string Format(DateTime timestamp, string name, Dictionary<string, object> content);

    // Ferme le fichier de log (ajoute les marqueurs de fin si nécessaire)
    // @param filePath - chemin du fichier à fermer
    void Close(string filePath) { }
}
