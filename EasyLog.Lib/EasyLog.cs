namespace EasyLog.Lib;

public class EasyLog
{
    private readonly ILogFormatter _formatter;
    private string _logPath;

    public EasyLog(ILogFormatter formatter, string logPath)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrWhiteSpace(logPath))
            throw new ArgumentException("Le chemin du fichier de log ne peut pas être null, vide ou whitespace.", nameof(logPath));

        _formatter = formatter;
        _logPath = logPath;

        EnsureDirectoryExists(_logPath);
    }

    public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        try
        {
            string formattedLog = _formatter.Format(timestamp, name, content);
            File.AppendAllText(_logPath, formattedLog + Environment.NewLine);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Erreur lors de l'écriture dans le fichier de log : {_logPath}",
                ex);
        }
    }

    public void SetLogPath(string newLogPath)
    {
        if (string.IsNullOrWhiteSpace(newLogPath))
            throw new ArgumentException("Le nouveau chemin ne peut pas être null, vide ou whitespace.", nameof(newLogPath));

        EnsureDirectoryExists(newLogPath);
        _logPath = newLogPath;
    }

    public string GetCurrentLogPath()
    {
        return _logPath;
    }


    private static void EnsureDirectoryExists(string logPath)
    {
        string? directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
