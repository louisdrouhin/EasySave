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
            throw new ArgumentException("The log directory path can't be null", nameof(logDirectory));

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
                $"Error when reopening the log file: {_logPath}",
                ex);
        }
    }

    private void EnsureFileIsOpen()
    {
        try
        {
            if (File.Exists(_logPath))
            {
                var content = File.ReadAllText(_logPath);
                if (content.EndsWith("]}"))
                {
                    var reopenedContent = content.Substring(0, content.Length - 2);
                    File.WriteAllText(_logPath, reopenedContent);
                    _isFirstEntry = false;
                }
            }
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error checking log file opening: {_logPath}",
                ex);
        }
    }

    private void NormalizePathsInContent(Dictionary<string, object> content)
    {
        var keysToUpdate = content.Keys
            .Where(k => k.Contains("Path", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToUpdate)
        {
            if (content[key] is string pathValue)
            {
                content[key] = ConvertToUncPath(pathValue);
            }
        }
    }

    private string ConvertToUncPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        try
        {
            if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                return path;

            var fullPath = Path.GetFullPath(path);

            if (fullPath.Length >= 2 && fullPath[1] == ':')
            {
                var drive = fullPath[0];
                var pathWithoutDrive = fullPath.Substring(2);
                return $"\\\\localhost\\{drive}$\\{pathWithoutDrive}";
            }
            return fullPath;
        }
        catch
        {
            return path;
        }
    }

    public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        CheckAndRotateIfNeeded();
        EnsureFileIsOpen();
        NormalizePathsInContent(content);

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
                $"Error while writing to the log file: {_logPath}",
                ex);
        }
    }

    private void CheckAndRotateIfNeeded()
    {
        DateTime todayDate = DateTime.Now.Date;

        if (todayDate != _currentDate)
        {
            Close();
            _currentDate = todayDate;
            _logPath = GetLogPathForDate(_currentDate);
            _isFirstEntry = true;
            InitializeJsonStructure();
        }
    }

    public void SetLogPath(string newLogDirectory)
    {
        if (string.IsNullOrWhiteSpace(newLogDirectory))
            throw new ArgumentException("The new path for the log file cannot be null, empty, or whitespace.", nameof(newLogDirectory));

        Close();

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

    private static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
