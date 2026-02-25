namespace EasyLog.Lib;

// Gestionnaire de logs avec support JSON et XML
// Crée un fichier de log par jour, manage les rotations, et utilise des formateurs pluggables
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

    // Initialise le gestionnaire de logs
    // Crée le dossier si absent et initialise la structure du fichier de log du jour
    // @param formatter - formateur JSON ou XML pour les entrées
    // @param logDirectory - répertoire où stocker les fichiers de log
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

    // Génère le chemin du fichier de log pour une date donnée
    // @param date - date pour laquelle générer le chemin du log
    // @returns chemin complet du fichier de log pour la date
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

    // Initialise la structure du fichier de log
    // Si le fichier n'existe pas, crée-le avec les en-têtes appropriés
    // Si le fichier existe, vérifie sa structure et la corrige si nécessaire pour permettre l'ajout de nouvelles entrées
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

    // Vérifie que le fichier de log est prêt à recevoir de nouvelles entrées
    // Si le fichier existe, vérifie sa structure et la corrige si nécessaire pour permettre l'ajout de nouvelles entrées
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

    // Normalise les paths dans le contenu des logs
    // Convertit les chemins locaux en chemins UNC pour assurer la compatibilité avec les systèmes de fichiers distants
    // @param content - dictionnaire de propriétés de l'entrée de log, potentiellement contenant des chemins à normaliser
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

    // Convertit un chemin local en chemin UNC
    // Si le chemin est déjà un chemin UNC, le retourne tel quel
    // @param path - chemin à convertir
    // @returns chemin UNC équivalent ou le chemin original si la conversion échoue
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

    // Écrit une entrée de log dans le fichier
    // Formate l'entrée avec le formateur configuré et l'ajoute au fichier de log
    // Si le jour a changé depuis la dernière écriture, effectue une rotation du fichier de log
    // @param timestamp - date/heure de l'entrée de log
    // @param name - nom du backup ou de l'événement à loguer
    // @param content - dictionnaire de propriétés de l'entrée de log
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

    // Vérifie si le jour a changé depuis la dernière écriture
    // Si le jour a changé, ferme le fichier de log actuel et initialise un nouveau fichier pour le nouveau jour
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

    // Permet de changer dynamiquement le répertoire de stockage des logs
    // Ferme le fichier de log actuel, met à jour le chemin, et initialise la structure du nouveau fichier de log
    // @param newLogDirectory - nouveau chemin du répertoire de logs
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

    // Récupère le chemin actuel du fichier de log
    // @returns chemin complet du fichier de log actuellement utilisé
    public string GetCurrentLogPath()
    {
        lock (_lock)
        {
            return _logPath;
        }
    }

    // Récupère le répertoire actuel où les fichiers de log sont stockés
    // @returns chemin du répertoire de logs
    public string GetLogDirectory()
    {
        lock (_lock)
        {
            return _logDirectory;
        }
    }

    // Ferme le fichier de log actuel en appelant la méthode de fermeture du formateur
    // Assure que les marqueurs de fin sont ajoutés si nécessaire pour garantir la validité du fichier de log
    public void Close()
    {
        lock (_lock)
        {
            CloseInternal();
        }
    }

    // Ferme le fichier de log actuel sans acquérir de lock (utilisé en interne lors de la rotation ou du changement de chemin)
    // Appelle la méthode de fermeture du formateur pour ajouter les marqueurs de fin si nécessaire
    private void CloseInternal()
    {
        _formatter.Close(_logPath);
    }

    // Assure que le répertoire de logs existe, et le crée s'il n'existe pas
    // @param directory - chemin du répertoire à vérifier/créer
    private static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
