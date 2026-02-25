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

// ViewModel for the Logs page
// Displays and manages log entries from JSON/XML files
public class LogsPageViewModel : ViewModelBase
{
    private readonly ConfigParser _configParser;
    private readonly JobManager _jobManager;
    private string _currentLogFilePath = "";
    private int _lastLoadedLogCount = 0;

    // Initializes the Logs page
    // Loads existing logs and subscribes to events
    // @param jobManager - central manager containing ConfigParser
    public LogsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _configParser = jobManager.ConfigParser;

        Logs = new ObservableCollection<LogEntryViewModel>();
        OpenFolderCommand = new RelayCommand(_ => OpenFolder());

        // Loads initial logs
        LoadLogs();

        // Subscribes to events
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        _jobManager.LogFormatChanged += OnLogFormatChanged;
        _jobManager.LogEntryWritten += OnLogEntryWritten;
    }


    // Observable collection of log entries
    public ObservableCollection<LogEntryViewModel> Logs { get; }

    // Number of log entries currently displayed
    private int _logCount;

    // Total number of loaded log entries
    public int LogCount
    {
        get => _logCount;
        private set => SetProperty(ref _logCount, value);
    }

    // Translated titles and labels
    public string HeaderTitle => LocalizationManager.Get("LogsPage_Title");
    public string OpenFolderLabel => LocalizationManager.Get("LogsPage_Button_OpenFolder");
    public string TotalLogsLabel => LocalizationManager.Get("LogsPage_TotalLogs");

    // Command to open the logs folder in the file explorer
    public ICommand OpenFolderCommand { get; }


    // Raised when a new log entry is added
    public event EventHandler? LogAdded;

    // Extracts individual JSON objects from a content string
    // @param content - string potentially containing multiple JSON objects
    // @returns list of valid JSON strings extracted from content
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

    // Opens the logs folder in the file explorer
    // Creates the folder if it doesn't exist, then uses the appropriate command based on the OS
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

    // Loads logs from the current file
    // Checks the configured log format, reads the corresponding file, and updates the observable collection
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

    // Loads logs from a JSON file
    // @param filePath - path to the JSON file to read
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
            // If parsing completely fails, log the beginning of the file content
            Debug.WriteLine($"[LogsPageViewModel.LoadJsonLogs] File content preview: {jsonContent?.Substring(0, Math.Min(200, jsonContent?.Length ?? 0)) ?? "null"}");
        }
    }

    // Loads logs from an XML file
    // @param filePath - path to the XML file to read
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

    // Updates the total number of displayed logs
    private void UpdateLogCount()
    {
        LogCount = Logs.Count;
    }

    // Handles new log entry written event
    private void OnLogEntryWritten(object? sender, string logLine)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Add(new LogEntryViewModel(logLine));
            UpdateLogCount();
            LogAdded?.Invoke(this, EventArgs.Empty);
        });
    }

    // Handles language change
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(OpenFolderLabel));
        OnPropertyChanged(nameof(TotalLogsLabel));
    }

    // Handles log format change
    private void OnLogFormatChanged(object? sender, LogFormatChangedEventArgs e)
    {
        Logs.Clear();
        _lastLoadedLogCount = 0;
        LoadLogs();
    }

    // Cleans up event subscriptions to prevent memory leaks
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        _jobManager.LogFormatChanged -= OnLogFormatChanged;
        _jobManager.LogEntryWritten -= OnLogEntryWritten;
    }
}
