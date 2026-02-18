namespace EasyLog.Lib;

public class EasyLog
{
    private readonly ILogFormatter _formatter;
    private string _logDirectory;
    private string _logPath;
    private bool _isFirstEntry;
    private DateTime _currentDate;
    private readonly string _fileExtension;
    private readonly string _entrySeparator;
    private readonly object _lock = new object();

    public EasyLog(ILogFormatter formatter, string logDirectory)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("The log directory path can't be null", nameof(logDirectory));

        _formatter = formatter;
        _logDirectory = logDirectory;
        _currentDate = DateTime.Now.Date;

        // Détecte le format basé sur le type de formatter
        bool isXmlFormat = formatter is XmlLogFormatter;
        _fileExtension = isXmlFormat ? "xml" : "json";
        _entrySeparator = isXmlFormat ? "" : ",";

        _logPath = GetLogPathForDate(_currentDate);
        _isFirstEntry = true;

        EnsureDirectoryExists(_logDirectory);
        InitializeLogStructure();
    }

    private string GetLogPathForDate(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var fileName = $"{dateStr}_logs.{_fileExtension}";
        var dailyLogPath = Path.Combine(_logDirectory, fileName);

        // Vérifie si un fichier de l'autre format existe
        string otherExtension = _fileExtension == "xml" ? "json" : "xml";
        var otherFormatPath = Path.Combine(_logDirectory, $"{dateStr}_logs.{otherExtension}");

        // Si le fichier du format actuel n'existe pas mais que l'autre format existe,
        // on crée un nouveau fichier dans le format actuel
        if (!File.Exists(dailyLogPath) && File.Exists(otherFormatPath))
        {
            // Le fichier sera créé par InitializeLogStructure
            return dailyLogPath;
        }

        return dailyLogPath;
    }

    private void InitializeLogStructure()
    {
        if (!File.Exists(_logPath))
        {
            bool isXml = _fileExtension == "xml";
            string header = isXml ? "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<logs>" : "{\"logs\":[";
            File.WriteAllText(_logPath, header);
            _isFirstEntry = true;
        }
        else
        {
            // Vérifions si le fichier contient déjà des entrées
            var content = File.ReadAllText(_logPath);
            bool isXml = _fileExtension == "xml";

            if (isXml)
            {
                // Vérifie si le fichier XML contient des entrées (cherche <logEntry>)
                _isFirstEntry = !content.Contains("<logEntry>");

                // Si le fichier est fermé, on le rouvre
                if (content.EndsWith("</logs>"))
                {
                    var reopenedContent = content.Substring(0, content.Length - 7);
                    File.WriteAllText(_logPath, reopenedContent);
                }
            }
            else
            {
                // Vérifie si le fichier JSON contient des entrées (cherche "timestamp")
                _isFirstEntry = !content.Contains("\"timestamp\"");

                // Si le fichier est fermé, on le rouvre
                if (content.EndsWith("]}"))
                {
                    var reopenedContent = content.Substring(0, content.Length - 2);
                    File.WriteAllText(_logPath, reopenedContent);
                }
            }
        }
    }

    private void EnsureFileIsOpen()
    {
        try
        {
            if (File.Exists(_logPath))
            {
                var content = File.ReadAllText(_logPath);
                bool isXml = _fileExtension == "xml";

                if (isXml)
                {
                    // Vérifie si le fichier contient des entrées                    _isFirstEntry = !content.Contains("<logEntry>");

                    // Si le fichier est fermé, on le rouvre
                    if (content.EndsWith("</logs>"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 7);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
                }
                else
                {
                    // Vérifie si le fichier contient des entrées
                    _isFirstEntry = !content.Contains("\"timestamp\"");

                    // Si le fichier est fermé, on le rouvre
                    if (content.EndsWith("]}"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 2);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
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
            // Si c'est déjà un chemin UNC, ne rien faire
            if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                return path;

            // Résoudre le chemin complet (relatif ou absolu)
            var fullPath = Path.GetFullPath(path);

            // Convertir en format UNC si c'est un chemin local avec lettre de lecteur
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

        lock (_lock)
        {
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
                    File.AppendAllText(_logPath, _entrySeparator + formattedLog);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    $"Error while writing to the log file: {_logPath}",
                    ex);
            }
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
            InitializeLogStructure();
        }
    }

    public void SetLogPath(string newLogDirectory)
    {
        if (string.IsNullOrWhiteSpace(newLogDirectory))
            throw new ArgumentException("The new path for the log file cannot be null, empty, or whitespace.", nameof(newLogDirectory));

        lock (_lock)
        {
            CloseInternal();

            _logDirectory = newLogDirectory;
            _currentDate = DateTime.Now.Date;
            _logPath = GetLogPathForDate(_currentDate);
            _isFirstEntry = true;

            EnsureDirectoryExists(_logDirectory);
            InitializeLogStructure();
        }
    }

    public string GetCurrentLogPath()
    {
        lock (_lock)
        {
            return _logPath;
        }
    }

    public string GetLogDirectory()
    {
        lock (_lock)
        {
            return _logDirectory;
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            CloseInternal();
        }
    }

    private void CloseInternal()
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
