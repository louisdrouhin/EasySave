namespace EasySave.Models;

// Représente une entrée de log pour un fichier sauvegardé
// Contient les détails de la sauvegarde (timestamp, chemins, taille, durée)
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public long FileSize { get; set; }
    public int TransferTimeMs { get; set; }

    // Crée une entrée de log pour une sauvegarde de fichier
    // @param timestamp - date/heure de la sauvegarde
    // @param backupName - nom du job de sauvegarde
    // @param sourcePath - chemin du fichier source
    // @param destinationPath - chemin du fichier destination
    // @param fileSize - taille du fichier en bytes
    // @param transferTimeMs - durée du transfert en millisecondes
    public LogEntry(
        DateTime timestamp,
        string backupName,
        string sourcePath,
        string destinationPath,
        long fileSize,
        int transferTimeMs)
    {
        Timestamp = timestamp;
        BackupName = backupName;
        SourcePath = sourcePath;
        DestinationPath = destinationPath;
        FileSize = fileSize;
        TransferTimeMs = transferTimeMs;
    }

    // Convertit l'entrée dans un format normalisé pour les logs
    // @returns tuple contenant timestamp, nom du backup et contenu sous forme de dictionnaire
    public (DateTime timestamp, string name, Dictionary<string, object> content) ToNormalizedFormat()
    {
        var content = new Dictionary<string, object>
        {
            { "sourcePath", SourcePath },
            { "destinationPath", DestinationPath },
            { "fileSize", FileSize },
            { "transferTimeMs", TransferTimeMs }
        };

        return (Timestamp, BackupName, content);
    }
}
