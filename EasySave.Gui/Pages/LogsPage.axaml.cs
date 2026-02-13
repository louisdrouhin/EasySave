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
using System.Text.Json;
using System.Threading;

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

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
                return;
            }

            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.{logFormat}";
            _currentLogFilePath = Path.Combine(logsPath, todayLogFileName);

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

            UpdateLogsCount();
            
            ScrollToBottom();
        }
        catch (Exception)
        {
        }
    }

    private void LoadJsonLogs(string filePath)
    {
        try
        {
            string jsonContent;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                jsonContent = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return;
            }

            _logs.Clear();

            var matches = System.Text.RegularExpressions.Regex.Matches(
                jsonContent,
                @"\{""timestamp"":""[^""]+""[^}]*\}",
                System.Text.RegularExpressions.RegexOptions.None);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string logEntry = match.Value;
                _logs.Add(new SimpleLogEntry
                {
                    LogText = logEntry
                });
            }
        }
        catch (Exception)
        {
        }
    }

    private void LoadXmlLogs(string filePath)
    {
        try
        {
        }
        catch (Exception)
        {
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
                Filter = "*.json", 
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
            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.json";

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
        var logsCountText = this.FindControl<TextBlock>("LogsCountText");
        if (logsCountText != null)
        {
            logsCountText.Text = _logs.Count.ToString();
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
