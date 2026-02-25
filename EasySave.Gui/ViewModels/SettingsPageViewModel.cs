using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel for the Settings page
// Manages configuration: logs, extensions, business applications, language, etc.
public class SettingsPageViewModel : ViewModelBase
{
    private readonly ConfigParser _configParser;
    private readonly JobManager _jobManager;
    private string _newPriorityExtensionText = "";
    private string _newEncryptionExtensionText = "";
    private string _newBusinessAppText = "";
    private string _maxConcurrentJobs = "";
    private long _largeFileSizeLimitKb;

    // Initializes the Settings page
    // Loads configuration and lists (extensions, apps, etc.)
    // @param jobManager - central manager containing ConfigParser
    public SettingsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _configParser = jobManager.ConfigParser;

        // Collections
        EncryptionExtensions = new ObservableCollection<ExtensionItemViewModel>();
        BusinessApps = new ObservableCollection<AppItemViewModel>();
        PriorityExtensions = new ObservableCollection<ExtensionItemViewModel>();

        // Commandes
        SetJsonFormatCommand = new RelayCommand(_ => SetLogFormat("json"));
        SetXmlFormatCommand = new RelayCommand(_ => SetLogFormat("xml"));
        SetFrenchLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("fr"));
        SetEnglishLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("en"));
        AddPriorityExtensionCommand = new RelayCommand(_ => AddPriorityExtension());
        AddEncryptionExtensionCommand = new RelayCommand(_ => AddEncryptionExtension());
        AddBusinessAppCommand = new RelayCommand(_ => AddBusinessApp());

        // Charge les données initiales
        _maxConcurrentJobs = _configParser.GetMaxConcurrentJobs().ToString();
        _largeFileSizeLimitKb = _configParser.GetLargeFileSizeLimitKb();

        RefreshEncryptionExtensions();
        RefreshBusinessApps();
        RefreshPriorityExtensions();

        // Subscribes to changes
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        _jobManager.LogFormatChanged += OnLogFormatChanged;
    }

    // Translated titles and subtitles
    public string HeaderTitle => LocalizationManager.Get("SettingsPage_Header_Title");
    public string HeaderSubtitle => LocalizationManager.Get("SettingsPage_Header_Subtitle");
    public string LogsSectionTitle => LocalizationManager.Get("SettingsPage_Section_Logs");
    public string StateSectionTitle => LocalizationManager.Get("SettingsPage_Section_State");
    public string EncryptionSectionTitle => LocalizationManager.Get("SettingsPage_Section_Encryption");
    public string BusinessAppsSectionTitle => LocalizationManager.Get("SettingsPage_Section_BusinessApps");
    public string PrioritySectionTitle => LocalizationManager.Get("SettingsPage_Section_Priority");
    public string PerformanceSectionTitle => LocalizationManager.Get("SettingsPage_Section_Performance");
    // Specific setting labels
    public string LogsPathLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Path");
    public string LogsFormatLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Format");
    public string ServerLogsEnabledLabel => LocalizationManager.Get("SettingsPage_Section_Logs_ServerEnabled");
    public string ServerLogsModeLabel => LocalizationManager.Get("SettingsPage_Section_Logs_ServerMode");
    public string ServerLogsHostLabel => LocalizationManager.Get("SettingsPage_Section_Logs_ServerHost");
    public string ServerLogsPortLabel => LocalizationManager.Get("SettingsPage_Section_Logs_ServerPort");
    public string StatePathLabel => LocalizationManager.Get("SettingsPage_Section_State_Path");
    public string ExtensionsLabel => LocalizationManager.Get("SettingsPage_Section_Encryption_Extensions");
    public string PriorityExtensionsLabel => LocalizationManager.Get("SettingsPage_Section_Priority_Extensions");
    public string AppsLabel => LocalizationManager.Get("SettingsPage_Section_BusinessApps_List");
    public string MaxConcurrentJobsLabel => LocalizationManager.Get("SettingsPage_Section_Performance_MaxConcurrentJobs");
    public string LargeFileSizeLimitLabel => LocalizationManager.Get("SettingsPage_Section_Performance_LargeFileSize");
    public string LanguageSectionTitle => LocalizationManager.Get("SettingsPage_Section_Language");
    public string CurrentLanguageLabel => LocalizationManager.Get("SettingsPage_Section_Language_Current");
    public string AboutSectionTitle => LocalizationManager.Get("SettingsPage_Section_About");
    public string VersionLabel => LocalizationManager.Get("SettingsPage_Section_About_Version");
    // Empty messages
    public string NoExtensionsText => LocalizationManager.Get("SettingsPage_NoExtensions");
    public string NoAppsText => LocalizationManager.Get("SettingsPage_NoApps");


    // Current configuration values
    public string LogsPathValue => Path.GetFullPath(_configParser.GetLogsPath());

    // Shows current log format in uppercase (JSON or XML)
    public string LogsFormatValue => _configParser.GetLogFormat().ToUpper();

    // EasyLog server configuration values
    public string ServerLogsEnabledValue => _configParser.GetEasyLogServerEnabled() ? "Enabled" : "Disabled";

    // Shows EasyLog server mode
    public string ServerLogsModeValue => _configParser.GetEasyLogServerMode();

    // Shows EasyLog server host address
    public string ServerLogsHostValue => _configParser.GetEasyLogServerHost();

    // Shows EasyLog server port number
    public string ServerLogsPortValue => _configParser.GetEasyLogServerPort().ToString();

    // Shows the state file path or "N/A" if not configured
    public string StatePathValue
    {
        get
        {
            var stateFilePath = _configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>();
            return !string.IsNullOrEmpty(stateFilePath) ? Path.GetFullPath(stateFilePath) : "N/A";
        }
    }

    // Shows current language (French or English) based on culture
    public string CurrentLanguageValue
    {
        get
        {
            string languageName = LocalizationManager.CurrentCulture.TwoLetterISOLanguageName == "fr"
                ? LocalizationManager.Get("Language_French")
                : LocalizationManager.Get("Language_English");
            return languageName;
        }
    }

    // Shows application version extracted from .cz.toml file or "Unknown" if not found
    public string VersionValue => GetApplicationVersion();

    // Properties related to new items to add (extensions, apps)
    public string NewPriorityExtensionText
    {
        get => _newPriorityExtensionText;
        set => SetProperty(ref _newPriorityExtensionText, value);
    }

    // Text entered for a new encryption extension to add
    public string NewEncryptionExtensionText
    {
        get => _newEncryptionExtensionText;
        set => SetProperty(ref _newEncryptionExtensionText, value);
    }

    // Text entered for a new business application to add
    public string NewBusinessAppText
    {
        get => _newBusinessAppText;
        set => SetProperty(ref _newBusinessAppText, value);
    }

    // Maximum number of concurrent jobs allowed, linked to configuration and saved on each change
    public string MaxConcurrentJobs
    {
        get => _maxConcurrentJobs;
        set
        {
            SetProperty(ref _maxConcurrentJobs, value);
            SaveMaxConcurrentJobs();
        }
    }

    // Size threshold in KB to consider a file as "large", linked to configuration and saved on each change
    public long LargeFileSizeLimitKb
    {
        get => _largeFileSizeLimitKb;
        set
        {
            SetProperty(ref _largeFileSizeLimitKb, value);
            SaveLargeFileSize();
        }
    }

    // Collections of extensions and business applications
    public ObservableCollection<ExtensionItemViewModel> EncryptionExtensions { get; }

    // Collection of configured business applications
    public ObservableCollection<AppItemViewModel> BusinessApps { get; }

    // Collection of configured priority extensions
    public ObservableCollection<ExtensionItemViewModel> PriorityExtensions { get; }

    // Commands related to user actions
    public ICommand SetJsonFormatCommand { get; }

    // Commands to change log format, language, and add extensions/apps
    public ICommand SetXmlFormatCommand { get; }

    // Commands to change application language
    public ICommand SetFrenchLanguageCommand { get; }

    // Command to switch application to English
    public ICommand SetEnglishLanguageCommand { get; }

    // Commands to add a priority extension, encryption extension, or business application
    public ICommand AddPriorityExtensionCommand { get; }

    // Commands to add an encryption extension
    public ICommand AddEncryptionExtensionCommand { get; }

    // Commands to add a business application
    public ICommand AddBusinessAppCommand { get; }

    // Methods to manage user actions and manipulate configuration
    private void SetLogFormat(string format)
    {
        try
        {
            _jobManager.SetLogFormat(format);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing log format: {ex.Message}");
        }
    }

    // Adds a new extension to the priority extensions list if it doesn't already exist, then saves and refreshes the displayed list
    private void AddPriorityExtension()
    {
        if (string.IsNullOrWhiteSpace(NewPriorityExtensionText))
            return;

        string extension = NewPriorityExtensionText.Trim().ToLower();
        if (!extension.StartsWith("."))
            extension = "." + extension;

        var currentExtensions = _configParser.GetPriorityExtensions();
        if (!currentExtensions.Contains(extension))
        {
            currentExtensions.Add(extension);
            SavePriorityExtensions(currentExtensions);
            NewPriorityExtensionText = "";
            RefreshPriorityExtensions();
        }
    }

    // Adds a new extension to the encryption extensions list if it doesn't already exist, then saves and refreshes the displayed list
    private void AddEncryptionExtension()
    {
        if (string.IsNullOrWhiteSpace(NewEncryptionExtensionText))
            return;

        string extension = NewEncryptionExtensionText.Trim().ToLower();
        if (!extension.StartsWith("."))
            extension = "." + extension;

        var currentExtensions = _configParser.GetEncryptionExtensions();
        if (!currentExtensions.Contains(extension))
        {
            currentExtensions.Add(extension);
            SaveEncryptionExtensions(currentExtensions);
            NewEncryptionExtensionText = "";
            RefreshEncryptionExtensions();
        }
    }

    // Removes an encryption extension from the configuration
    private void RemoveEncryptionExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetEncryptionExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SaveEncryptionExtensions(currentExtensions);
        RefreshEncryptionExtensions();
    }

    // Saves the encryption extensions list to the configuration by modifying the JSON file and persisting changes
    private void SaveEncryptionExtensions(List<string> extensions)
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            if (configSection["encryption"] is System.Text.Json.Nodes.JsonObject encryptionSection)
            {
                var array = new System.Text.Json.Nodes.JsonArray();
                foreach (var ext in extensions)
                {
                    array.Add(ext);
                }
                encryptionSection["extensions"] = array;
                _configParser.EditAndSaveConfig(configObject);
            }
        }
    }

    // Refreshes the displayed encryption extensions collection
    private void RefreshEncryptionExtensions()
    {
        EncryptionExtensions.Clear();
        var extensions = _configParser.GetEncryptionExtensions();
        foreach (var ext in extensions)
        {
            EncryptionExtensions.Add(new ExtensionItemViewModel(ext, RemoveEncryptionExtension));
        }
    }

    // Adds a new business application to the configuration
    private void AddBusinessApp()
    {
        if (string.IsNullOrWhiteSpace(NewBusinessAppText))
            return;

        string appName = NewBusinessAppText.Trim().ToLower();
        var currentApps = _configParser.GetBusinessApplications();
        if (!currentApps.Contains(appName))
        {
            currentApps.Add(appName);
            SaveBusinessApps(currentApps);
            NewBusinessAppText = "";
            RefreshBusinessApps();
        }
    }

    // Removes a business application from the configuration
    private void RemoveBusinessApp(AppItemViewModel vm)
    {
        var currentApps = _configParser.GetBusinessApplications();
        currentApps.Remove(vm.DisplayText);
        SaveBusinessApps(currentApps);
        RefreshBusinessApps();
    }

    // Saves the business applications list to the configuration by modifying the JSON file and persisting changes
    private void SaveBusinessApps(List<string> apps)
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (var app in apps)
            {
                array.Add(app);
            }
            configSection["businessApplications"] = array;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

    // Refreshes the displayed business applications collection
    private void RefreshBusinessApps()
    {
        BusinessApps.Clear();
        var apps = _configParser.GetBusinessApplications();
        foreach (var app in apps)
        {
            BusinessApps.Add(new AppItemViewModel(app, RemoveBusinessApp));
        }
    }

    // Removes a priority extension from the configuration
    private void RemovePriorityExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetPriorityExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SavePriorityExtensions(currentExtensions);
        RefreshPriorityExtensions();
    }

    // Saves the priority extensions list to the configuration by modifying the JSON file and persisting changes
    private void SavePriorityExtensions(List<string> extensions)
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (var ext in extensions)
            {
                array.Add(ext);
            }
            configSection["priorityExtensions"] = array;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

    // Refreshes the displayed priority extensions collection
    private void RefreshPriorityExtensions()
    {
        PriorityExtensions.Clear();
        var extensions = _configParser.GetPriorityExtensions();
        foreach (var ext in extensions)
        {
            PriorityExtensions.Add(new ExtensionItemViewModel(ext, RemovePriorityExtension));
        }
    }

    // Saves the maximum number of concurrent jobs to the configuration by modifying the JSON file and persisting changes
    private void SaveMaxConcurrentJobs()
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            if (string.IsNullOrWhiteSpace(_maxConcurrentJobs) || !int.TryParse(_maxConcurrentJobs, out int value))
            {
                // Don't save invalid values
                return;
            }

            configSection["maxConcurrentJobs"] = value;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

    // Saves the size threshold for large files to the configuration by modifying the JSON file and persisting changes
    private void SaveLargeFileSize()
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            configSection["largeFileSizeLimitKb"] = _largeFileSizeLimitKb;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

    // Reads the .cz.toml file at the project root to extract the application version
    private string GetApplicationVersion()
    {
        try
        {
            string projectRoot = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo currentDir = new DirectoryInfo(projectRoot);

            while (currentDir != null && currentDir.Parent != null)
            {
                string czTomlPath = Path.Combine(currentDir.FullName, ".cz.toml");
                if (File.Exists(czTomlPath))
                {
                    string content = File.ReadAllText(czTomlPath);
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(content, @"version\s*=\s*""([^""]+)""");
                    if (versionMatch.Success)
                    {
                        return versionMatch.Groups[1].Value;
                    }
                    break;
                }
                currentDir = currentDir.Parent;
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading version: {ex.Message}");
            return "Unknown";
        }
    }

    // Handles events to update the interface when language or log format changes
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(LogsSectionTitle));
        OnPropertyChanged(nameof(StateSectionTitle));
        OnPropertyChanged(nameof(EncryptionSectionTitle));
        OnPropertyChanged(nameof(BusinessAppsSectionTitle));
        OnPropertyChanged(nameof(PrioritySectionTitle));
        OnPropertyChanged(nameof(PerformanceSectionTitle));
        OnPropertyChanged(nameof(LogsPathLabel));
        OnPropertyChanged(nameof(LogsFormatLabel));
        OnPropertyChanged(nameof(ServerLogsEnabledLabel));
        OnPropertyChanged(nameof(ServerLogsModeLabel));
        OnPropertyChanged(nameof(ServerLogsHostLabel));
        OnPropertyChanged(nameof(ServerLogsPortLabel));
        OnPropertyChanged(nameof(StatePathLabel));
        OnPropertyChanged(nameof(ExtensionsLabel));
        OnPropertyChanged(nameof(PriorityExtensionsLabel));
        OnPropertyChanged(nameof(AppsLabel));
        OnPropertyChanged(nameof(MaxConcurrentJobsLabel));
        OnPropertyChanged(nameof(LargeFileSizeLimitLabel));
        OnPropertyChanged(nameof(LanguageSectionTitle));
        OnPropertyChanged(nameof(CurrentLanguageLabel));
        OnPropertyChanged(nameof(AboutSectionTitle));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(NoExtensionsText));
        OnPropertyChanged(nameof(NoAppsText));
        OnPropertyChanged(nameof(CurrentLanguageValue));
    }

    // Updates the log format display when it changes in the JobManager
    private void OnLogFormatChanged(object? sender, LogFormatChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LogsFormatValue));
    }

    // Unsubscribes from events to prevent memory leaks when the ViewModel is destroyed
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        _jobManager.LogFormatChanged -= OnLogFormatChanged;
    }
}
