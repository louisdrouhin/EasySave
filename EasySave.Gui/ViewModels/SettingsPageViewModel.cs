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

// ViewModel pour la page des paramètres
// Gère la configuration: logs, extensions, applications métier, langue, etc.
public class SettingsPageViewModel : ViewModelBase
{
    private readonly ConfigParser _configParser;
    private readonly JobManager _jobManager;
    private string _newPriorityExtensionText = "";
    private string _newEncryptionExtensionText = "";
    private string _newBusinessAppText = "";
    private string _maxConcurrentJobs = "";
    private long _largeFileSizeLimitKb;

    // Initialise la page des paramètres
    // Charge la configuration et les listes (extensions, apps, etc.)
    // @param jobManager - gestionnaire central contenant ConfigParser
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

        // S'abonne aux changements
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        _jobManager.LogFormatChanged += OnLogFormatChanged;
    }

    // Titres et sous-titres traduits
    public string HeaderTitle => LocalizationManager.Get("SettingsPage_Header_Title");
    public string HeaderSubtitle => LocalizationManager.Get("SettingsPage_Header_Subtitle");
    public string LogsSectionTitle => LocalizationManager.Get("SettingsPage_Section_Logs");
    public string StateSectionTitle => LocalizationManager.Get("SettingsPage_Section_State");
    public string EncryptionSectionTitle => LocalizationManager.Get("SettingsPage_Section_Encryption");
    public string BusinessAppsSectionTitle => LocalizationManager.Get("SettingsPage_Section_BusinessApps");
    public string PrioritySectionTitle => LocalizationManager.Get("SettingsPage_Section_Priority");
    public string PerformanceSectionTitle => LocalizationManager.Get("SettingsPage_Section_Performance");
    // Labels spécifiques des paramètres
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
    // Messages vides
    public string NoExtensionsText => LocalizationManager.Get("SettingsPage_NoExtensions");
    public string NoAppsText => LocalizationManager.Get("SettingsPage_NoApps");


    // Valeurs actuelles de la configuration
    public string LogsPathValue => Path.GetFullPath(_configParser.GetLogsPath());

    // Affiche le format de log actuel en majuscules (JSON ou XML)
    public string LogsFormatValue => _configParser.GetLogFormat().ToUpper();

    // Affiche le chemin du fichier d'état ou "N/A" si non configuré
    public string StatePathValue
    {
        get
        {
            var stateFilePath = _configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>();
            return !string.IsNullOrEmpty(stateFilePath) ? Path.GetFullPath(stateFilePath) : "N/A";
        }
    }

    // Affiche la langue actuelle (Français ou Anglais) basée sur la culture
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

    // Affiche la version de l'application extraite du fichier .cz.toml ou "Unknown" si non trouvée
    public string VersionValue => GetApplicationVersion();

    // Propriétés liées aux nouveaux éléments à ajouter (extensions, apps)
    public string NewPriorityExtensionText
    {
        get => _newPriorityExtensionText;
        set => SetProperty(ref _newPriorityExtensionText, value);
    }

    // Texte saisi pour une nouvelle extension d'encryption à ajouter
    public string NewEncryptionExtensionText
    {
        get => _newEncryptionExtensionText;
        set => SetProperty(ref _newEncryptionExtensionText, value);
    }

    // Texte saisi pour une nouvelle application métier à ajouter
    public string NewBusinessAppText
    {
        get => _newBusinessAppText;
        set => SetProperty(ref _newBusinessAppText, value);
    }

    // Nombre maximum de jobs concurrents autorisés, lié à la configuration et sauvegardé à chaque changement
    public string MaxConcurrentJobs
    {
        get => _maxConcurrentJobs;
        set
        {
            SetProperty(ref _maxConcurrentJobs, value);
            SaveMaxConcurrentJobs();
        }
    }

    // Seuil de taille en KB pour considérer un fichier comme "gros", lié à la configuration et sauvegardé à chaque changement
    public long LargeFileSizeLimitKb
    {
        get => _largeFileSizeLimitKb;
        set
        {
            SetProperty(ref _largeFileSizeLimitKb, value);
            SaveLargeFileSize();
        }
    }

    // Collections d'extensions et d'applications métier
    public ObservableCollection<ExtensionItemViewModel> EncryptionExtensions { get; }

    // Collection des applications métier configurées
    public ObservableCollection<AppItemViewModel> BusinessApps { get; }

    // Collection des extensions prioritaires configurées
    public ObservableCollection<ExtensionItemViewModel> PriorityExtensions { get; }

    // Commandes liées aux actions de l'utilisateur
    public ICommand SetJsonFormatCommand { get; }

    // Commandes pour changer le format de log, la langue, et ajouter des extensions/apps
    public ICommand SetXmlFormatCommand { get; }

    // Commandes pour changer la langue de l'application
    public ICommand SetFrenchLanguageCommand { get; }

    // Commande pour passer l'application en anglais
    public ICommand SetEnglishLanguageCommand { get; }

    // Commandes pour ajouter une extension prioritaire, une extension d'encryption, ou une application métier
    public ICommand AddPriorityExtensionCommand { get; }

    // Commandes pour ajouter une extension d'encryption
    public ICommand AddEncryptionExtensionCommand { get; }

    // Commandes pour ajouter une application métier
    public ICommand AddBusinessAppCommand { get; }

    // Méthodes pour gérer les actions de l'utilisateur et manipuler la configuration
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

    // Ajoute une nouvelle extension à la liste des extensions prioritaires si elle n'existe pas déjà, puis sauvegarde et rafraîchit la liste affichée
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

    // Ajoute une nouvelle extension à la liste des extensions d'encryption si elle n'existe pas déjà, puis sauvegarde et rafraîchit la liste affichée
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

    // Supprime une extension d'encryption de la configuration
    private void RemoveEncryptionExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetEncryptionExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SaveEncryptionExtensions(currentExtensions);
        RefreshEncryptionExtensions();
    }

    // Sauvegarde la liste des extensions d'encryption dans la configuration en modifiant le fichier JSON et en persistant les changements
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

    // Rafraîchit la collection d'extensions d'encryption affichée
    private void RefreshEncryptionExtensions()
    {
        EncryptionExtensions.Clear();
        var extensions = _configParser.GetEncryptionExtensions();
        foreach (var ext in extensions)
        {
            EncryptionExtensions.Add(new ExtensionItemViewModel(ext, RemoveEncryptionExtension));
        }
    }

    // Ajoute une nouvelle application métier à la configuration
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

    // Supprime une application métier de la configuration
    private void RemoveBusinessApp(AppItemViewModel vm)
    {
        var currentApps = _configParser.GetBusinessApplications();
        currentApps.Remove(vm.DisplayText);
        SaveBusinessApps(currentApps);
        RefreshBusinessApps();
    }

    // Sauvegarde la liste des applications métier dans la configuration en modifiant le fichier JSON et en persistant les changements
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

    // Rafraîchit la collection d'applications métier affichée
    private void RefreshBusinessApps()
    {
        BusinessApps.Clear();
        var apps = _configParser.GetBusinessApplications();
        foreach (var app in apps)
        {
            BusinessApps.Add(new AppItemViewModel(app, RemoveBusinessApp));
        }
    }

    // Supprime une extension prioritaire de la configuration
    private void RemovePriorityExtension(ExtensionItemViewModel vm)
    {
        var currentExtensions = _configParser.GetPriorityExtensions();
        currentExtensions.Remove(vm.DisplayText);
        SavePriorityExtensions(currentExtensions);
        RefreshPriorityExtensions();
    }

    // Sauvegarde la liste des extensions prioritaires dans la configuration en modifiant le fichier JSON et en persistant les changements
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

    // Rafraîchit la collection d'extensions prioritaires affichée
    private void RefreshPriorityExtensions()
    {
        PriorityExtensions.Clear();
        var extensions = _configParser.GetPriorityExtensions();
        foreach (var ext in extensions)
        {
            PriorityExtensions.Add(new ExtensionItemViewModel(ext, RemovePriorityExtension));
        }
    }

    // Sauvegarde le nombre maximum de jobs concurrents dans la configuration en modifiant le fichier JSON et en persistant les changements
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

    // Sauvegarde le seuil de taille pour les gros fichiers dans la configuration en modifiant le fichier JSON et en persistant les changements
    private void SaveLargeFileSize()
    {
        if (_configParser.Config is System.Text.Json.Nodes.JsonObject configObject &&
            configObject["config"] is System.Text.Json.Nodes.JsonObject configSection)
        {
            configSection["largeFileSizeLimitKb"] = _largeFileSizeLimitKb;
            _configParser.EditAndSaveConfig(configObject);
        }
    }

    // Lit le fichier .cz.toml à la racine du projet pour extraire la version de l'application
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

    // Gestion des événements pour mettre à jour l'interface lorsque la langue ou le format de log change
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

    // Met à jour l'affichage du format de log lorsque celui-ci change dans le JobManager
    private void OnLogFormatChanged(object? sender, LogFormatChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LogsFormatValue));
    }

    // Se désabonne des événements pour éviter les fuites de mémoire lorsque le ViewModel est détruit
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        _jobManager.LogFormatChanged -= OnLogFormatChanged;
    }
}
