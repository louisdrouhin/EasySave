namespace EasySave.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public long FileSize { get; set; }
    public int TransferTimeMs { get; set; }

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
