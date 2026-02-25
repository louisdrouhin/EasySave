using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Input;
using System.Xml;
using Avalonia.Threading;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel pour la page des logs
// Affiche et gère les entrées de logs depuis les fichiers JSON/XML
public class LogsPageViewModel : ViewModelBase
{
    private readonly ConfigParser _configParser;
    private readonly JobManager _jobManager;
    private string _currentLogFilePath = "";
    private int _lastLoadedLogCount = 0;

    // Initialise la page des logs
    // Charge les logs existants et s'abonne aux événements
    // @param jobManager - gestionnaire central contenant ConfigParser
    public LogsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _configParser = jobManager.ConfigParser;

        Logs = new ObservableCollection<LogEntryViewModel>();
        OpenFolderCommand = new RelayCommand(_ => OpenFolder());

        // Charge les logs initiaux
        LoadLogs();

        // S'abonne aux événements
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        _jobManager.LogFormatChanged += OnLogFormatChanged;
        _jobManager.LogEntryWritten += OnLogEntryWritten;
    }


    // Collection observable des entrées de log
    public ObservableCollection<LogEntryViewModel> Logs { get; }

    // Nombre d'entrées de log actuellement affichées
    private int _logCount;

    // Nombre total d'entrées de log chargées
    public int LogCount
    {
        get => _logCount;
        private set => SetProperty(ref _logCount, value);
    }

    // Titres et labels traduits
    public string HeaderTitle => LocalizationManager.Get("LogsPage_Title");
    public string OpenFolderLabel => LocalizationManager.Get("LogsPage_Button_OpenFolder");
    public string TotalLogsLabel => LocalizationManager.Get("LogsPage_TotalLogs");

    // Commande pour ouvrir le dossier des logs dans l'explorateur
    public ICommand OpenFolderCommand { get; }


    // Déclenché quand une nouvelle entrée de log est ajoutée
    public event EventHandler? LogAdded;

    // Extrait les objets JSON individuels d'une chaîne de contenu
    // @param content - chaîne contenant potentiellement plusieurs objets JSON
    // @returns liste de chaînes JSON valides extraites du contenu
    private List<string> ExtractJsonObjects(string content)
    {
        List<string> objects = new List<string>();
        int braceCount = 0;
        int startIndex = -1;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '{')
            {
                if (braceCount == 0)
                    startIndex = i;
                braceCount++;
            }
            else if (c == '}')
            {
                braceCount--;
                if (braceCount == 0 && startIndex != -1)
                {
                    try
                    {
                        string obj = content.Substring(startIndex, i - startIndex + 1);
                        // Validate that it's valid JSON
                        using (JsonDocument.Parse(obj))
                        {
                            objects.Add(obj);
                        }
                    }
                    catch
                    {
                        // Ignore invalid objects
                    }
                    startIndex = -1;
                }
            }
        }

        return objects;
    }

    // Ouvre le dossier des logs dans l'explorateur de fichiers
    // Crée le dossier s'il n'existe pas, puis utilise la commande appropriée selon le système d'exploitation
    private void OpenFolder()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            string absolutePath = Path.GetFullPath(logsPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = absolutePath,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", absolutePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", absolutePath);
            }
        }
        catch (Exception)
        {
        }
    }

    // Charge les logs depuis le fichier actuel
    // Vérifie le format de log configuré, lit le fichier correspondant, et met à jour la collection observable
    private void LoadLogs()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();
            string logFormat = _configParser.GetLogFormat();

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
                return;
            }

            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.{logFormat}";
            string newLogFilePath = Path.Combine(logsPath, todayLogFileName);

            // If log file changed (new day or format change), reset
            if (newLogFilePath != _currentLogFilePath)
            {
                _currentLogFilePath = newLogFilePath;
                _lastLoadedLogCount = 0;
                Logs.Clear();
            }

            if (!File.Exists(_currentLogFilePath))
            {
                return;
            }

            if (logFormat == "json")
            {
                LoadJsonLogs(_currentLogFilePath);
            }
            else if (logFormat == "xml")
            {
                LoadXmlLogs(_currentLogFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogsPageViewModel.LoadLogs] ERROR: {ex.Message}");
        }
    }

    // Charge les logs depuis un fichier JSON
    // @param filePath - chemin du fichier JSON à lire
    private void LoadJsonLogs(string filePath)
    {
        string? jsonContent = null;
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                jsonContent = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
                return;

            jsonContent = jsonContent.TrimEnd();

            jsonContent = jsonContent.TrimStart(' ', '\t', '\n', '\r', ',');

            List<string> logStrings = ExtractJsonObjects(jsonContent);

            if (logStrings.Count == 0)
            {
                if (!jsonContent.StartsWith("{\"logs\":["))
                {
                    if (jsonContent.StartsWith("{"))
                    {
                        jsonContent = "{\"logs\":[" + jsonContent.TrimStart('{');
                    }
                    else
                    {
                        jsonContent = "{\"logs\":[" + jsonContent;
                    }
                }

                if (!jsonContent.EndsWith("]}"))
                {
                    if (jsonContent.EndsWith(","))
                    {
                        jsonContent = jsonContent.Substring(0, jsonContent.Length - 1);
                    }
                    jsonContent += "]}";
                }

                try
                {
                    using (var doc = JsonDocument.Parse(jsonContent))
                    {
                        if (doc.RootElement.TryGetProperty("logs", out JsonElement logsArray))
                        {
                            foreach (JsonElement logEntry in logsArray.EnumerateArray())
                            {
                                try
                                {
                                    string logJson = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
                                    logStrings.Add(logJson);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs] Error serializing entry: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs] Failed to parse wrapped JSON: {ex.Message}");
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    int previousCount = _lastLoadedLogCount;
                    int newLogsCount = logStrings.Count - previousCount;

                    if (newLogsCount > 0)
                    {
                        for (int i = previousCount; i < logStrings.Count; i++)
                        {
                            Logs.Add(new LogEntryViewModel(logStrings[i]));
                        }

                        _lastLoadedLogCount = logStrings.Count;
                        UpdateLogCount();
                    }
                    else if (logStrings.Count < previousCount)
                    {
                        // File was reset, reload all
                        Logs.Clear();
                        _lastLoadedLogCount = 0;

                        foreach (string logJson in logStrings)
                        {
                            Logs.Add(new LogEntryViewModel(logJson));
                        }

                        _lastLoadedLogCount = logStrings.Count;
                        UpdateLogCount();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs.UIThread] ERROR: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs] ERROR: {ex.Message}");
            // Si le parsing échoue complètement, enregistrer le contenu du début du fichier
            Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs] File content preview: {jsonContent?.Substring(0, Math.Min(200, jsonContent?.Length ?? 0)) ?? "null"}");
        }
    }

    // Charge les logs depuis un fichier XML
    // @param filePath - chemin du fichier XML à lire
    private void LoadXmlLogs(string filePath)
    {
        try
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            List<string> logStrings = new List<string>();
            XmlNodeList logEntries = xmlDoc.GetElementsByTagName("Log");

            foreach (XmlNode logEntry in logEntries)
            {
                try
                {
                    string logText = $"[{logEntry.SelectSingleNode("Timestamp")?.InnerText}] {logEntry.SelectSingleNode("Name")?.InnerText}";
                    logStrings.Add(logText);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LogsPageViewModel.LoadXmlLogs] Error parsing entry: {ex.Message}");
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    int previousCount = _lastLoadedLogCount;
                    int newLogsCount = logStrings.Count - previousCount;

                    if (newLogsCount > 0)
                    {
                        for (int i = previousCount; i < logStrings.Count; i++)
                        {
                            Logs.Add(new LogEntryViewModel(logStrings[i]));
                        }

                        _lastLoadedLogCount = logStrings.Count;
                        UpdateLogCount();
                    }
                    else if (logStrings.Count < previousCount)
                    {
                        Logs.Clear();
                        _lastLoadedLogCount = 0;

                        foreach (string logText in logStrings)
                        {
                            Logs.Add(new LogEntryViewModel(logText));
                        }

                        _lastLoadedLogCount = logStrings.Count;
                        UpdateLogCount();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LogsPageViewModel.LoadXmlLogs.UIThread] ERROR: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogsPageViewModel.LoadXmlLogs] ERROR: {ex.Message}");
        }
    }

    // Met à jour le nombre total de logs affichés
    private void UpdateLogCount()
    {
        LogCount = Logs.Count;
    }

    // Gère l'événement de nouvelle entrée de log écrite
    private void OnLogEntryWritten(object? sender, string logLine)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Add(new LogEntryViewModel(logLine));
            UpdateLogCount();
            LogAdded?.Invoke(this, EventArgs.Empty);
        });
    }

    // Gère le changement de langue
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(OpenFolderLabel));
        OnPropertyChanged(nameof(TotalLogsLabel));
    }

    // Gère le changement de format de log
    private void OnLogFormatChanged(object? sender, LogFormatChangedEventArgs e)
    {
        Logs.Clear();
        _lastLoadedLogCount = 0;
        LoadLogs();
    }

    // Nettoie les abonnements aux événements pour éviter les fuites de mémoire
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        _jobManager.LogFormatChanged -= OnLogFormatChanged;
        _jobManager.LogEntryWritten -= OnLogEntryWritten;
    }
}
