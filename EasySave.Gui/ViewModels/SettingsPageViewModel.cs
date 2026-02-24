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

public class SettingsPageViewModel : ViewModelBase
{
    private readonly ConfigParser _configParser;
    private readonly JobManager _jobManager;
    private string _newPriorityExtensionText = "";
    private string _newEncryptionExtensionText = "";
    private string _newBusinessAppText = "";
    private string _maxConcurrentJobs = "";
    private long _largeFileSizeLimitKb;

    public SettingsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _configParser = jobManager.ConfigParser;

        // Collections
        EncryptionExtensions = new ObservableCollection<ExtensionItemViewModel>();
        BusinessApps = new ObservableCollection<AppItemViewModel>();
        PriorityExtensions = new ObservableCollection<ExtensionItemViewModel>();

        // Commands
        SetJsonFormatCommand = new RelayCommand(_ => SetLogFormat("json"));
        SetXmlFormatCommand = new RelayCommand(_ => SetLogFormat("xml"));
        SetFrenchLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("fr"));
        SetEnglishLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("en"));
        AddPriorityExtensionCommand = new RelayCommand(_ => AddPriorityExtension());
        AddEncryptionExtensionCommand = new RelayCommand(_ => AddEncryptionExtension());
        AddBusinessAppCommand = new RelayCommand(_ => AddBusinessApp());

        // Load initial data
        _maxConcurrentJobs = _configParser.GetMaxConcurrentJobs().ToString();
        _largeFileSizeLimitKb = _configParser.GetLargeFileSizeLimitKb();

        RefreshEncryptionExtensions();
        RefreshBusinessApps();
        RefreshPriorityExtensions();

        // Subscribe to language changes
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        _jobManager.LogFormatChanged += OnLogFormatChanged;
    }

    #region Properties - Localized Labels (computed, no backing field)

    public string HeaderTitle => LocalizationManager.Get("SettingsPage_Header_Title");
    public string HeaderSubtitle => LocalizationManager.Get("SettingsPage_Header_Subtitle");
    public string LogsSectionTitle => LocalizationManager.Get("SettingsPage_Section_Logs");
    public string StateSectionTitle => LocalizationManager.Get("SettingsPage_Section_State");
    public string EncryptionSectionTitle => LocalizationManager.Get("SettingsPage_Section_Encryption");
    public string BusinessAppsSectionTitle => LocalizationManager.Get("SettingsPage_Section_BusinessApps");
    public string PrioritySectionTitle => LocalizationManager.Get("SettingsPage_Section_Priority");
    public string PerformanceSectionTitle => LocalizationManager.Get("SettingsPage_Section_Performance");
    public string LogsPathLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Path");
    public string LogsFormatLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Format");
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
    public string NoExtensionsText => LocalizationManager.Get("SettingsPage_NoExtensions");
    public string NoAppsText => LocalizationManager.Get("SettingsPage_NoApps");

    #endregion

    #region Properties - Config Values

    public string LogsPathValue => Path.GetFullPath(_configParser.GetLogsPath());

    public string LogsFormatValue => _configParser.GetLogFormat().ToUpper();

    public string StatePathValue
    {
        get
        {
            var stateFilePath = _configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>();
            return !string.IsNullOrEmpty(stateFilePath) ? Path.GetFullPath(stateFilePath) : "N/A";
        }
    }

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

    public string VersionValue => GetApplicationVersion();

    public string NewPriorityExtensionText
    {
        get => _newPriorityExtensionText;
        set => SetProperty(ref _newPriorityExtensionText, value);
    }

    public string NewEncryptionExtensionText
    {
        get => _newEncryptionExtensionText;
        set => SetProperty(ref _newEncryptionExtensionText, value);
    }

    public string NewBusinessAppText
    {
        get => _newBusinessAppText;
        set => SetProperty(ref _newBusinessAppText, value);
    }

    public string MaxConcurrentJobs
    {
        get => _maxConcurrentJobs;
        set
        {
            SetProperty(ref _maxConcurrentJobs, value);
            SaveMaxConcurrentJobs();
        }
    }

    public long LargeFileSizeLimitKb
    {
        get => _largeFileSizeLimitKb;
        set
        {
            SetProperty(ref _largeFileSizeLimitKb, value);
            SaveLargeFileSize();
        }
    }

    #endregion

    #region Collections

    public ObservableCollection<ExtensionItemViewModel> EncryptionExtensions { get; }
    public ObservableCollection<AppItemViewModel> BusinessApps { get; }
    public ObservableCollection<ExtensionItemViewModel> PriorityExtensions { get; }

    #endregion

    #region Commands

    public ICommand SetJsonFormatCommand { get; }
    public ICommand SetXmlFormatCommand { get; }
    public ICommand SetFrenchLanguageCommand { get; }
    public ICommand SetEnglishLanguageCommand { get; }
    public ICommand AddPriorityExtensionCommand { get; }
    public ICommand AddEncryptionExtensionCommand { get; }
    public ICommand AddBusinessAppCommand { get; }

    #endregion

    #region Methods

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

    private void RemoveEncryptionExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetEncryptionExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SaveEncryptionExtensions(currentExtensions);
        RefreshEncryptionExtensions();
    }

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

    private void RefreshEncryptionExtensions()
    {
        EncryptionExtensions.Clear();
        var extensions = _configParser.GetEncryptionExtensions();
        foreach (var ext in extensions)
        {
            EncryptionExtensions.Add(new ExtensionItemViewModel(ext, RemoveEncryptionExtension));
        }
    }

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

    private void RemoveBusinessApp(AppItemViewModel vm)
    {
        var currentApps = _configParser.GetBusinessApplications();
        currentApps.Remove(vm.DisplayText);
        SaveBusinessApps(currentApps);
        RefreshBusinessApps();
    }

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

    private void RefreshBusinessApps()
    {
        BusinessApps.Clear();
        var apps = _configParser.GetBusinessApplications();
        foreach (var app in apps)
        {
            BusinessApps.Add(new AppItemViewModel(app, RemoveBusinessApp));
        }
    }

    private void RemovePriorityExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetPriorityExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SavePriorityExtensions(currentExtensions);
        RefreshPriorityExtensions();
    }

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

    private void RefreshPriorityExtensions()
    {
        PriorityExtensions.Clear();
        var extensions = _configParser.GetPriorityExtensions();
        foreach (var ext in extensions)
        {
            PriorityExtensions.Add(new ExtensionItemViewModel(ext, RemovePriorityExtension));
        }
    }

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

    private void SaveLargeFileSize()
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            configSection["largeFileSizeLimitKb"] = _largeFileSizeLimitKb;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

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

    private void OnLogFormatChanged(object? sender, LogFormatChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LogsFormatValue));
    }

    #endregion

    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        _jobManager.LogFormatChanged -= OnLogFormatChanged;
    }
}
