using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml;

namespace EasySave.GUI.Pages;

public class SimpleLogEntry
{
    public string LogText { get; set; } = string.Empty;
}

public partial class LogsPage : UserControl
{
    private readonly ObservableCollection<SimpleLogEntry> _logs;
    private readonly ConfigParser _configParser;
    private readonly JobManager? _jobManager;
    private FileSystemWatcher? _fileWatcher;
    private string _currentLogFilePath = string.Empty;
    private Timer? _reloadTimer;
    private readonly object _reloadLock = new object();
    private int _lastLoadedLogCount = 0; // Pour ne charger que les nouveaux logs

    public LogsPage() : this(null, null)
    {
    }

    public LogsPage(ConfigParser? configParser, JobManager? jobManager = null)
    {
        _logs = new ObservableCollection<SimpleLogEntry>();
        _configParser = configParser ?? new ConfigParser("config.json");
        _jobManager = jobManager;

        InitializeComponent();

        LocalizationManager.LanguageChanged += OnLanguageChanged;

        // Subscribe to log format change events
        if (_jobManager != null)
        {
            _jobManager.LogFormatChanged += OnLogFormatChangedEvent;
        }

        var titleText = this.FindControl<TextBlock>("TitleText");
        if (titleText != null) titleText.Text = LocalizationManager.Get("LogsPage_Title");

        var openFolderButton = this.FindControl<Button>("OpenFolderButton");
        if (openFolderButton != null)
        {
            openFolderButton.Content = LocalizationManager.Get("LogsPage_Button_OpenFolder");
            openFolderButton.Click += OnOpenFolderClick;
        }

        var totalLogsLabel = this.FindControl<TextBlock>("TotalLogsLabelText");
        if (totalLogsLabel != null) totalLogsLabel.Text = LocalizationManager.Get("LogsPage_TotalLogs");

        var logsListBox = this.FindControl<ListBox>("LogsListBox");
        if (logsListBox != null)
        {
            logsListBox.ItemsSource = _logs;
            Debug.WriteLine($"[LogsPage] ListBox ItemsSource set to _logs collection");
        }
        else
        {
            Debug.WriteLine($"[LogsPage] ERROR: Could not find LogsListBox control!");
        }

        LoadLogs();

        StartFileWatcher();

        ScrollToBottom();

        this.Loaded += (s, e) => ScrollToBottom();
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
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

    private void LoadLogs()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();
            string logFormat = _configParser.GetLogFormat();

            Debug.WriteLine($"[LogsPage.LoadLogs] Starting - logsPath: {logsPath}, format: {logFormat}");

            if (!Directory.Exists(logsPath))
            {
                Debug.WriteLine($"[LogsPage.LoadLogs] Directory does not exist: {logsPath}");
                Directory.CreateDirectory(logsPath);
                return;
            }

            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.{logFormat}";
            string newLogFilePath = Path.Combine(logsPath, todayLogFileName);

            // Si le fichier de logs a changé (nouveau jour ou changement de format), réinitialiser
            if (newLogFilePath != _currentLogFilePath)
            {
                Debug.WriteLine($"[LogsPage.LoadLogs] Log file changed from '{_currentLogFilePath}' to '{newLogFilePath}', resetting");
                _currentLogFilePath = newLogFilePath;
                _lastLoadedLogCount = 0;
                _logs.Clear();
            }

            Debug.WriteLine($"[LogsPage.LoadLogs] Looking for file: {_currentLogFilePath}");
            Debug.WriteLine($"[LogsPage.LoadLogs] File exists: {File.Exists(_currentLogFilePath)}");

            if (!File.Exists(_currentLogFilePath))
            {
                Debug.WriteLine($"[LogsPage.LoadLogs] File not found, returning");
                return;
            }

            Debug.WriteLine($"[LogsPage.LoadLogs] Calling Load{logFormat}Logs");

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
            Debug.WriteLine($"[LogsPage.LoadLogs] ERROR: {ex.Message}");
            Debug.WriteLine($"[LogsPage.LoadLogs] Stack: {ex.StackTrace}");
        }
    }

    private void LoadJsonLogs(string filePath)
    {
        try
        {
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] START - file: {filePath}");

            string jsonContent;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                jsonContent = reader.ReadToEnd();
            }

            Debug.WriteLine($"[LogsPage.LoadJsonLogs] Read {jsonContent.Length} characters");

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Debug.WriteLine("[LogsPage.LoadJsonLogs] JSON content is empty - EXITING");
                return;
            }

            // Debug: show start and end of file
            string start = jsonContent.Length > 100 ? jsonContent.Substring(0, 100) : jsonContent;
            string end = jsonContent.Length > 100 ? jsonContent.Substring(jsonContent.Length - 100) : jsonContent;
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] File START: {start}");
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] File END: {end}");

            // Fix incomplete JSON (EasyLog doesn't close the array/object while writing)
            jsonContent = jsonContent.TrimEnd();

            // Check if file starts with {"logs":[
            if (!jsonContent.StartsWith("{\"logs\":["))
            {
                Debug.WriteLine($"[LogsPage.LoadJsonLogs] JSON doesn't start with correct structure, adding prefix");
                // If it doesn't start properly but has content, wrap it
                if (jsonContent.StartsWith("{"))
                {
                    // Might be individual log entries, wrap them
                    jsonContent = "{\"logs\":[" + jsonContent.TrimStart('{');
                }
                else
                {
                    jsonContent = "{\"logs\":[" + jsonContent;
                }
            }

            if (!jsonContent.EndsWith("]}"))
            {
                Debug.WriteLine($"[LogsPage.LoadJsonLogs] JSON is incomplete, adding closing brackets");
                // Remove trailing comma if exists
                if (jsonContent.EndsWith(","))
                {
                    jsonContent = jsonContent.Substring(0, jsonContent.Length - 1);
                }
                jsonContent += "]}";
            }

            Debug.WriteLine($"[LogsPage.LoadJsonLogs] Parsing JSON...");

            // Parse the JSON structure: {"logs": [...]}
            List<string> logStrings = new List<string>();
            using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
            {
                Debug.WriteLine($"[LogsPage.LoadJsonLogs] JSON parsed successfully");

                if (doc.RootElement.TryGetProperty("logs", out System.Text.Json.JsonElement logsArray))
                {
                    int arrayLength = logsArray.GetArrayLength();
                    Debug.WriteLine($"[LogsPage.LoadJsonLogs] Found logs array with {arrayLength} entries");

                    // Extract all log entries to strings while the document is still alive
                    Debug.WriteLine($"[LogsPage.LoadJsonLogs] Extracting log entries...");
                    foreach (System.Text.Json.JsonElement logEntry in logsArray.EnumerateArray())
                    {
                        try
                        {
                            // Convert each log entry to JSON string for display
                            string logJson = System.Text.Json.JsonSerializer.Serialize(logEntry, new System.Text.Json.JsonSerializerOptions
                            {
                                WriteIndented = false
                            });
                            logStrings.Add(logJson);
                        }
                        catch (Exception innerEx)
                        {
                            Debug.WriteLine($"[LogsPage.LoadJsonLogs] Error serializing log entry: {innerEx.Message}");
                        }
                    }
                    Debug.WriteLine($"[LogsPage.LoadJsonLogs] Extracted {logStrings.Count} log strings");
                }
                else
                {
                    Debug.WriteLine("[LogsPage.LoadJsonLogs] 'logs' property not found in JSON");
                }
            }

            // Now dispatch to UI thread with the extracted strings
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] Dispatching to UI thread...");
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Au lieu de tout recharger, ne charger que les nouveaux logs
                    int previousCount = _lastLoadedLogCount;
                    int newLogsCount = logStrings.Count - previousCount;

                    Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] START - Current logs: {_logs.Count}, Last loaded: {previousCount}, Total in file: {logStrings.Count}, New logs: {newLogsCount}");

                    if (newLogsCount > 0)
                    {
                        // Ajouter seulement les nouveaux logs
                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Adding {newLogsCount} new logs");

                        for (int i = previousCount; i < logStrings.Count; i++)
                        {
                            _logs.Add(new SimpleLogEntry
                            {
                                LogText = logStrings[i]
                            });
                        }

                        _lastLoadedLogCount = logStrings.Count;

                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Finished adding new logs, total now: {_logs.Count}");
                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Calling UpdateLogsCount()");
                        UpdateLogsCount();
                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Calling ScrollToBottom()");
                        ScrollToBottom();
                    }
                    else if (logStrings.Count < previousCount)
                    {
                        // Le fichier a été remplacé ou réinitialisé, recharger tout
                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Log file was reset, reloading all logs");
                        _logs.Clear();
                        _lastLoadedLogCount = 0;

                        foreach (string logJson in logStrings)
                        {
                            _logs.Add(new SimpleLogEntry
                            {
                                LogText = logJson
                            });
                        }

                        _lastLoadedLogCount = logStrings.Count;
                        UpdateLogsCount();
                        ScrollToBottom();
                    }
                    else
                    {
                        Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] No new logs to add");
                    }

                    Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] COMPLETE");
                }
                catch (Exception uiEx)
                {
                    Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] ERROR in UI thread: {uiEx.Message}");
                    Debug.WriteLine($"[LogsPage.LoadJsonLogs.UIThread] Stack: {uiEx.StackTrace}");
                }
            });
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] UI thread dispatch posted");
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] END");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] ERROR: {ex.Message}");
            Debug.WriteLine($"[LogsPage.LoadJsonLogs] Stack trace: {ex.StackTrace}");
        }
    }

    private void LoadXmlLogs(string filePath)
    {
        try
        {
            string xmlContent;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                xmlContent = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return;
            }

            xmlContent = xmlContent.Trim();
            if (!xmlContent.EndsWith("</logs>"))
            {
                xmlContent += "</logs>";
            }

            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            var logEntries = doc.GetElementsByTagName("logEntry");

            // Dispatch to UI thread
            Dispatcher.UIThread.Post(() =>
            {
                _logs.Clear();

                foreach (XmlElement entry in logEntries)
                {
                    var timestamp = entry.SelectSingleNode("timestamp")?.InnerText ?? "";
                    var name = entry.SelectSingleNode("name")?.InnerText ?? "";
                    var content = entry.SelectSingleNode("content");

                    var logDict = new Dictionary<string, object>
                    {
                        { "timestamp", timestamp },
                        { "name", name },
                        { "content", new Dictionary<string, object>() }
                    };

                    if (content != null && content.ChildNodes.Count > 0)
                    {
                        var contentDict = (Dictionary<string, object>)logDict["content"];
                        foreach (XmlElement child in content.ChildNodes)
                        {
                            contentDict[child.Name] = child.InnerText;
                        }
                    }

                    string logJson = JsonSerializer.Serialize(logDict);

                    _logs.Add(new SimpleLogEntry
                    {
                        LogText = logJson
                    });
                }

                UpdateLogsCount();
                ScrollToBottom();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading XML logs: {ex.Message}");
        }
    }

    private void StartFileWatcher()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            _fileWatcher = new FileSystemWatcher(logsPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*_logs.*",
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnLogFileChanged;
            _fileWatcher.Created += OnLogFileChanged;
        }
        catch (Exception)
        {
        }
    }

    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            string currentFormat = _configParser.GetLogFormat();
            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.{currentFormat}";

            if (Path.GetFileName(e.FullPath) == todayLogFileName)
            {
                lock (_reloadLock)
                {
                    _reloadTimer?.Dispose();
                    _reloadTimer = new Timer(_ =>
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            try
                            {
                                LoadLogs();
                            }
                            catch (Exception)
                            {
                            }
                        });
                    }, null, 200, Timeout.Infinite);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void UpdateLogsCount()
    {
        Debug.WriteLine($"[LogsPage.UpdateLogsCount] Current count: {_logs.Count}");
        var logsCountText = this.FindControl<TextBlock>("LogsCountText");
        if (logsCountText != null)
        {
            logsCountText.Text = _logs.Count.ToString();
            Debug.WriteLine($"[LogsPage.UpdateLogsCount] Updated UI text to: {_logs.Count}");
        }
        else
        {
            Debug.WriteLine($"[LogsPage.UpdateLogsCount] ERROR: LogsCountText control not found!");
        }
    }

    private void ScrollToBottom()
    {
        try
        {
            var logsListBox = this.FindControl<ListBox>("LogsListBox");
            if (logsListBox != null && _logs.Count > 0)
            {
                Thread.Sleep(50);

                logsListBox.SelectedIndex = _logs.Count - 1;
                logsListBox.ScrollIntoView(_logs[_logs.Count - 1]);
            }
        }
        catch (Exception)
        {
        }
    }

    private void OnLanguageChanged(object? sender, EasySave.Core.Localization.LanguageChangedEventArgs e)
    {
        try
        {
            var titleText = this.FindControl<TextBlock>("TitleText");
            if (titleText != null) titleText.Text = LocalizationManager.Get("LogsPage_Title");

            var openFolderButton = this.FindControl<Button>("OpenFolderButton");
            if (openFolderButton != null) openFolderButton.Content = LocalizationManager.Get("LogsPage_Button_OpenFolder");

            var totalLogsLabel = this.FindControl<TextBlock>("TotalLogsLabelText");
            if (totalLogsLabel != null) totalLogsLabel.Text = LocalizationManager.Get("LogsPage_TotalLogs");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating language in LogsPage: {ex.Message}");
        }
    }

    private void OnLogFormatChangedEvent(object? sender, EasySave.Core.LogFormatChangedEventArgs e)
    {
        try
        {
            LoadLogs();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reloading logs on format change: {ex.Message}");
        }
    }

    ~LogsPage()
    {
        _fileWatcher?.Dispose();
        _reloadTimer?.Dispose();
    }
}
