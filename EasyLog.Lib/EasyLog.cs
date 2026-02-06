namespace EasyLog.Lib;

public class EasyLog
{
    private readonly ILogFormatter _formatter;
    private string _logDirectory;
    private string _logPath;
    private bool _isFirstEntry;
    private DateTime _currentDate;

    public EasyLog(ILogFormatter formatter, string logDirectory)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("Le chemin du dossier des logs ne peut pas être null, vide ou whitespace.", nameof(logDirectory));

        _formatter = formatter;
        _logDirectory = logDirectory;
        _currentDate = DateTime.Now.Date;
        _logPath = GetLogPathForDate(_currentDate);
        _isFirstEntry = true;

        EnsureDirectoryExists(_logDirectory);
        InitializeJsonStructure();
    }

    private string GetLogPathForDate(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var fileName = $"{dateStr}_logs.json";
        var dailyLogPath = Path.Combine(_logDirectory, fileName);
        return dailyLogPath;
    }

    private void InitializeJsonStructure()
    {
        if (!File.Exists(_logPath))
        {
            File.WriteAllText(_logPath, "{\"logs\":[");
            _isFirstEntry = true;
        }
        else
        {
            ReopenClosedJsonFile();
            _isFirstEntry = false;
        }
    }

    private void ReopenClosedJsonFile()
    {
        try
        {
            var content = File.ReadAllText(_logPath);
            if (content.EndsWith("]}"))
            {
                var reopenedContent = content.Substring(0, content.Length - 2);
                File.WriteAllText(_logPath, reopenedContent);
                _isFirstEntry = false;
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Erreur lors de la réouverture du fichier de log : {_logPath}",
                ex);
        }
    }

    public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        CheckAndRotateIfNeeded();

        try
        {
            string formattedLog = _formatter.Format(timestamp, name, content);

            if (_isFirstEntry)
            {
                File.AppendAllText(_logPath, formattedLog);
                _isFirstEntry = false;
            }
            else
            {
                File.AppendAllText(_logPath, "," + formattedLog);
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Erreur lors de l'écriture dans le fichier de log : {_logPath}",
                ex);
        }
    }

    private void CheckAndRotateIfNeeded()
    {
        DateTime todayDate = DateTime.Now.Date;

        if (todayDate != _currentDate)
        {
            CloseJsonStructure();
            _currentDate = todayDate;
            _logPath = GetLogPathForDate(_currentDate);
            _isFirstEntry = true;
            InitializeJsonStructure();
        }
    }

    public void SetLogPath(string newLogDirectory)
    {
        if (string.IsNullOrWhiteSpace(newLogDirectory))
            throw new ArgumentException("Le nouveau chemin du dossier des logs ne peut pas être null, vide ou whitespace.", nameof(newLogDirectory));

        CloseJsonStructure();

        _logDirectory = newLogDirectory;
        _currentDate = DateTime.Now.Date;
        _logPath = GetLogPathForDate(_currentDate);
        _isFirstEntry = true;

        EnsureDirectoryExists(_logDirectory);
        InitializeJsonStructure();
    }

    public string GetCurrentLogPath()
    {
        return _logPath;
    }

    public string GetLogDirectory()
    {
        return _logDirectory;
    }

    public void Close()
    {
        _formatter.Close(_logPath);
    }

    private void CloseJsonStructure()
    {
        try
        {
            if (File.Exists(_logPath))
            {
                var content = File.ReadAllText(_logPath);
                if (!content.EndsWith("]}"))
                {
                    File.AppendAllText(_logPath, "]}");
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Erreur lors de la fermeture du fichier de log : {_logPath}",
                ex);
        }
    }

    private static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
