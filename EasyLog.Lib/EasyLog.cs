namespace EasyLog.Lib;

// Log manager with JSON and XML support
// Creates one log file per day, manages rotations, and uses pluggable formatters
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

    // Initializes the log manager
    // Creates the folder if missing and initializes the day's log file structure
    // @param formatter - JSON or XML formatter for entries
    // @param logDirectory - directory to store log files
    public EasyLog(ILogFormatter formatter, string logDirectory)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("The log directory path can't be null", nameof(logDirectory));

        _formatter = formatter;
        _logDirectory = logDirectory;
        _currentDate = DateTime.Now.Date;

        System.Diagnostics.Debug.WriteLine($"[EasyLog] Constructor called");
        System.Diagnostics.Debug.WriteLine($"[EasyLog] Log directory: {_logDirectory}");
        System.Diagnostics.Debug.WriteLine($"[EasyLog] Log directory is rooted: {Path.IsPathRooted(_logDirectory)}");

        bool isXmlFormat = formatter is XmlLogFormatter;
        _fileExtension = isXmlFormat ? "xml" : "json";
        _entrySeparator = isXmlFormat ? "" : ",";

        _logPath = GetLogPathForDate(_currentDate);
        System.Diagnostics.Debug.WriteLine($"[EasyLog] Log file path: {_logPath}");
        Console.WriteLine($"[EasyLog] Log file will be created at: {_logPath}");

        _isFirstEntry = true;

        EnsureDirectoryExists(_logDirectory);
        System.Diagnostics.Debug.WriteLine($"[EasyLog] Directory ensured: {Directory.Exists(_logDirectory)}");
        Console.WriteLine($"[EasyLog] Directory exists: {Directory.Exists(_logDirectory)}");

        InitializeLogStructure();
        System.Diagnostics.Debug.WriteLine($"[EasyLog] Log structure initialized, file exists: {File.Exists(_logPath)}");
        Console.WriteLine($"[EasyLog] Log file exists after init: {File.Exists(_logPath)}");
    }

    // Generates the log file path for a given date
    // @param date - date for which to generate the log path
    // @returns full path to the log file for the date
    private string GetLogPathForDate(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var fileName = $"{dateStr}_logs.{_fileExtension}";
        var dailyLogPath = Path.Combine(_logDirectory, fileName);

        string otherExtension = _fileExtension == "xml" ? "json" : "xml";
        var otherFormatPath = Path.Combine(_logDirectory, $"{dateStr}_logs.{otherExtension}");

        if (!File.Exists(dailyLogPath) && File.Exists(otherFormatPath))
        {
            return dailyLogPath;
        }

        return dailyLogPath;
    }

    // Initializes the log file structure
    // If the file doesn't exist, creates it with appropriate headers
    // If the file exists, checks its structure and corrects it if necessary to allow adding new entries
    private void InitializeLogStructure()
    {
        try
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
                var content = File.ReadAllText(_logPath);
                bool isXml = _fileExtension == "xml";

                if (isXml)
                {
                    _isFirstEntry = !content.Contains("<logEntry>");

                    if (content.EndsWith("</logs>"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 7);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
                }
                else
                {
                    _isFirstEntry = !content.Contains("\"timestamp\"");

                    content = content.Trim();

                    if (content.StartsWith(","))
                    {
                        content = content.TrimStart(' ', '\t', '\n', '\r', ',');
                        if (!content.StartsWith("{\"logs\":["))
                        {
                            content = "{\"logs\":[" + content;
                        }
                        File.WriteAllText(_logPath, content);
                    }
                    else if (content.StartsWith("["))
                    {
                        if (content.EndsWith("]"))
                        {
                            content = "{\"logs\":" + content;
                        }
                        File.WriteAllText(_logPath, content);
                    }
                    else if (content.StartsWith("{") && !content.StartsWith("{\"logs\":["))
                    {
                        content = "{\"logs\":[" + content;
                        File.WriteAllText(_logPath, content);
                    }

                    if (content.EndsWith("]}"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 2);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EasyLog] Error initializing log structure: {ex.Message}");
        }
    }

    // Checks that the log file is ready to receive new entries
    // If the file exists, checks its structure and corrects it if necessary to allow adding new entries
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
                    _isFirstEntry = !content.Contains("<logEntry>");

                    if (content.EndsWith("</logs>"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 7);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
                }
                else
                {
                    _isFirstEntry = !content.Contains("\"timestamp\"");

                    content = content.Trim();

                    if (content.StartsWith(","))
                    {
                        content = content.TrimStart(' ', '\t', '\n', '\r', ',');
                        if (!content.StartsWith("{\"logs\":["))
                        {
                            content = "{\"logs\":[" + content;
                        }
                        File.WriteAllText(_logPath, content);
                    }
                    else if (content.StartsWith("["))
                    {
                        if (content.EndsWith("]"))
                        {
                            content = "{\"logs\":" + content;
                        }
                        File.WriteAllText(_logPath, content);
                    }
                    else if (content.StartsWith("{") && !content.StartsWith("{\"logs\":["))
                    {
                        content = "{\"logs\":[" + content;
                        File.WriteAllText(_logPath, content);
                    }

                    if (content.EndsWith("]}"))
                    {
                        var reopenedContent = content.Substring(0, content.Length - 2);
                        File.WriteAllText(_logPath, reopenedContent);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EasyLog] Error ensuring file is open: {ex.Message}");
        }
    }

    // Normalizes paths in log content
    // Converts local paths to UNC paths to ensure compatibility with remote file systems
    // @param content - dictionary of log entry properties, potentially containing paths to normalize
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

    // Converts a local path to a UNC path
    // If the path is already a UNC path, returns it as is
    // @param path - path to convert
    // @returns equivalent UNC path or original path if conversion fails
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

    // Writes a log entry to the file
    // Formats the entry with the configured formatter and adds it to the log file
    // If the day has changed since the last write, performs log file rotation
    // @param timestamp - date/time of the log entry
    // @param name - name of the backup or event to log
    // @param content - dictionary of log entry properties
    public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        System.Diagnostics.Debug.WriteLine($"[EasyLog] Write called: {name} at {timestamp:HH:mm:ss.fff}");
        Console.WriteLine($"[EasyLog] Write called: {name} for content with {content.Count} items");

        lock (_lock)
        {
            try
            {
                CheckAndRotateIfNeeded();
                EnsureFileIsOpen();
                NormalizePathsInContent(content);

                string formattedLog = _formatter.Format(timestamp, name, content);
                System.Diagnostics.Debug.WriteLine($"[EasyLog] Formatted log length: {formattedLog.Length} chars");

                if (_isFirstEntry)
                {
                    File.AppendAllText(_logPath, formattedLog);
                    _isFirstEntry = false;
                    System.Diagnostics.Debug.WriteLine($"[EasyLog] First entry written to: {_logPath}");
                    Console.WriteLine($"[EasyLog] ✓ First entry written to: {_logPath}");
                }
                else
                {
                    File.AppendAllText(_logPath, _entrySeparator + formattedLog);
                    System.Diagnostics.Debug.WriteLine($"[EasyLog] Entry appended to: {_logPath}");
                    Console.WriteLine($"[EasyLog] ✓ Entry appended ({name})");
                }
            }
            catch (IOException ex)
            {
                var errorMsg = $"[EasyLog] IO Error writing log: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = $"[EasyLog] Unexpected error: {ex.Message}";
                var stackMsg = $"[EasyLog] Stack trace: {ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                System.Diagnostics.Debug.WriteLine(stackMsg);
                Console.WriteLine(errorMsg);
                Console.WriteLine(stackMsg);
            }
        }
    }

    // Checks if the day has changed since the last write
    // If the day has changed, closes the current log file and initializes a new file for the new day
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

    // Allows dynamically changing the log storage directory
    // Closes the current log file, updates the path, and initializes the new log file structure
    // @param newLogDirectory - new path to the logs directory
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

    // Gets the current log file path
    // @returns full path to the log file currently in use
    public string GetCurrentLogPath()
    {
        lock (_lock)
        {
            return _logPath;
        }
    }

    // Gets the current directory where log files are stored
    // @returns path to the logs directory
    public string GetLogDirectory()
    {
        lock (_lock)
        {
            return _logDirectory;
        }
    }

    // Closes the current log file by calling the formatter's close method
    // Ensures that end markers are added if necessary to guarantee log file validity
    public void Close()
    {
        lock (_lock)
        {
            CloseInternal();
        }
    }

    // Closes the current log file without acquiring a lock (used internally during rotation or path changes)
    // Calls the formatter's close method to add end markers if necessary
    private void CloseInternal()
    {
        _formatter.Close(_logPath);
    }

    // Ensures that the logs directory exists, and creates it if it doesn't
    // @param directory - path to the directory to check/create
    private static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
