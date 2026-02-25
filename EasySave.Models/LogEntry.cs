namespace EasySave.Models;

// Represents a log entry for a backed-up file
// Contains backup details (timestamp, paths, size, duration)
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public long FileSize { get; set; }
    public int TransferTimeMs { get; set; }

    // Creates a log entry for a file backup
    // @param timestamp - date/time of backup
    // @param backupName - name of the backup job
    // @param sourcePath - path of source file
    // @param destinationPath - path of destination file
    // @param fileSize - file size in bytes
    // @param transferTimeMs - transfer duration in milliseconds
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

    // Converts entry to normalized format for logs
    // @returns tuple containing timestamp, backup name and content as dictionary
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
