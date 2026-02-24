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

    public SettingsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _configParser = jobManager.ConfigParser;

        // Collections
        EncryptionExtensions = new ObservableCollection<string>(_configParser.GetEncryptionExtensions());
        BusinessApps = new ObservableCollection<string>(_configParser.GetBusinessApplications());
        PriorityExtensions = new ObservableCollection<ExtensionItemViewModel>();

        // Commands
        SetJsonFormatCommand = new RelayCommand(_ => SetLogFormat("json"));
        SetXmlFormatCommand = new RelayCommand(_ => SetLogFormat("xml"));
        SetFrenchLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("fr"));
        SetEnglishLanguageCommand = new RelayCommand(_ => LocalizationManager.SetLanguage("en"));
        AddPriorityExtensionCommand = new RelayCommand(_ => AddPriorityExtension());

        // Load initial data
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
    public string LogsPathLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Path");
    public string LogsFormatLabel => LocalizationManager.Get("SettingsPage_Section_Logs_Format");
    public string StatePathLabel => LocalizationManager.Get("SettingsPage_Section_State_Path");
    public string ExtensionsLabel => LocalizationManager.Get("SettingsPage_Section_Encryption_Extensions");
    public string PriorityExtensionsLabel => LocalizationManager.Get("SettingsPage_Section_Priority_Extensions");
    public string AppsLabel => LocalizationManager.Get("SettingsPage_Section_BusinessApps_List");
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

    #endregion

    #region Collections

    public ObservableCollection<string> EncryptionExtensions { get; }
    public ObservableCollection<string> BusinessApps { get; }
    public ObservableCollection<ExtensionItemViewModel> PriorityExtensions { get; }

    #endregion

    #region Commands

    public ICommand SetJsonFormatCommand { get; }
    public ICommand SetXmlFormatCommand { get; }
    public ICommand SetFrenchLanguageCommand { get; }
    public ICommand SetEnglishLanguageCommand { get; }
    public ICommand AddPriorityExtensionCommand { get; }

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
        OnPropertyChanged(nameof(LogsPathLabel));
        OnPropertyChanged(nameof(LogsFormatLabel));
        OnPropertyChanged(nameof(StatePathLabel));
        OnPropertyChanged(nameof(ExtensionsLabel));
        OnPropertyChanged(nameof(PriorityExtensionsLabel));
        OnPropertyChanged(nameof(AppsLabel));
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
