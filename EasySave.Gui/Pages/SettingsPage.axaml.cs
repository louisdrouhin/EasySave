using Avalonia.Controls;
using EasySave.Core.Localization;
using EasySave.Models;
using System;
using System.IO;
using System.Linq;

namespace EasySave.GUI.Pages;

public partial class SettingsPage : UserControl
{
    private readonly ConfigParser _configParser;

    public SettingsPage() : this(null)
    {
    }

    public SettingsPage(ConfigParser? configParser)
    {
        _configParser = configParser ?? new ConfigParser("config.json");
        InitializeComponent();

        LocalizationManager.LanguageChanged += OnLanguageChangedEvent;

        var headerTitleText = this.FindControl<TextBlock>("HeaderTitleText");
        if (headerTitleText != null) headerTitleText.Text = LocalizationManager.Get("SettingsPage_Header_Title");

        var headerSubtitleText = this.FindControl<TextBlock>("HeaderSubtitleText");
        if (headerSubtitleText != null) headerSubtitleText.Text = LocalizationManager.Get("SettingsPage_Header_Subtitle");

        var logsSectionTitle = this.FindControl<TextBlock>("LogsSectionTitle");
        if (logsSectionTitle != null) logsSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Logs");

        var stateSectionTitle = this.FindControl<TextBlock>("StateSectionTitle");
        if (stateSectionTitle != null) stateSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_State");

        var encryptionSectionTitle = this.FindControl<TextBlock>("EncryptionSectionTitle");
        if (encryptionSectionTitle != null) encryptionSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Encryption");

        var businessAppsSectionTitle = this.FindControl<TextBlock>("BusinessAppsSectionTitle");
        if (businessAppsSectionTitle != null) businessAppsSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_BusinessApps");

        var logsPathLabel = this.FindControl<TextBlock>("LogsPathLabel");
        if (logsPathLabel != null) logsPathLabel.Text = LocalizationManager.Get("SettingsPage_Section_Logs_Path");

        var logsFormatLabel = this.FindControl<TextBlock>("LogsFormatLabel");
        if (logsFormatLabel != null) logsFormatLabel.Text = LocalizationManager.Get("SettingsPage_Section_Logs_Format");

        var statePathLabel = this.FindControl<TextBlock>("StatePathLabel");
        if (statePathLabel != null) statePathLabel.Text = LocalizationManager.Get("SettingsPage_Section_State_Path");

        var extensionsLabel = this.FindControl<TextBlock>("ExtensionsLabel");
        if (extensionsLabel != null) extensionsLabel.Text = LocalizationManager.Get("SettingsPage_Section_Encryption_Extensions");

        var appsLabel = this.FindControl<TextBlock>("AppsLabel");
        if (appsLabel != null) appsLabel.Text = LocalizationManager.Get("SettingsPage_Section_BusinessApps_List");

        var languageSectionTitle = this.FindControl<TextBlock>("LanguageSectionTitle");
        if (languageSectionTitle != null) languageSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Language");

        var currentLanguageLabel = this.FindControl<TextBlock>("CurrentLanguageLabel");
        if (currentLanguageLabel != null) currentLanguageLabel.Text = LocalizationManager.Get("SettingsPage_Section_Language_Current");

        var frenchButton = this.FindControl<Button>("FrenchButton");
        if (frenchButton != null) frenchButton.Click += OnFrenchClick;

        var englishButton = this.FindControl<Button>("EnglishButton");
        if (englishButton != null) englishButton.Click += OnEnglishClick;

        var aboutSectionTitle = this.FindControl<TextBlock>("AboutSectionTitle");
        if (aboutSectionTitle != null) aboutSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_About");

        var versionLabel = this.FindControl<TextBlock>("VersionLabel");
        if (versionLabel != null) versionLabel.Text = LocalizationManager.Get("SettingsPage_Section_About_Version");

        PopulateData();
    }

    private void OnFrenchClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChangeLanguage("fr");
    }

    private void OnEnglishClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChangeLanguage("en");
    }

    private void ChangeLanguage(string languageCode)
    {
        try
        {
            LocalizationManager.SetLanguage(languageCode);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing language: {ex.Message}");
        }
    }

    private void OnLanguageChangedEvent(object? sender, EasySave.Core.Localization.LanguageChangedEventArgs e)
    {
        try
        {
            RefreshUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing SettingsPage on language change: {ex.Message}");
        }
    }

    private void RefreshUI()
    {
        var headerTitleText = this.FindControl<TextBlock>("HeaderTitleText");
        if (headerTitleText != null) headerTitleText.Text = LocalizationManager.Get("SettingsPage_Header_Title");

        var headerSubtitleText = this.FindControl<TextBlock>("HeaderSubtitleText");
        if (headerSubtitleText != null) headerSubtitleText.Text = LocalizationManager.Get("SettingsPage_Header_Subtitle");

        var logsSectionTitle = this.FindControl<TextBlock>("LogsSectionTitle");
        if (logsSectionTitle != null) logsSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Logs");

        var stateSectionTitle = this.FindControl<TextBlock>("StateSectionTitle");
        if (stateSectionTitle != null) stateSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_State");

        var encryptionSectionTitle = this.FindControl<TextBlock>("EncryptionSectionTitle");
        if (encryptionSectionTitle != null) encryptionSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Encryption");

        var businessAppsSectionTitle = this.FindControl<TextBlock>("BusinessAppsSectionTitle");
        if (businessAppsSectionTitle != null) businessAppsSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_BusinessApps");

        var languageSectionTitle = this.FindControl<TextBlock>("LanguageSectionTitle");
        if (languageSectionTitle != null) languageSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_Language");

        var logsPathLabel = this.FindControl<TextBlock>("LogsPathLabel");
        if (logsPathLabel != null) logsPathLabel.Text = LocalizationManager.Get("SettingsPage_Section_Logs_Path");

        var logsFormatLabel = this.FindControl<TextBlock>("LogsFormatLabel");
        if (logsFormatLabel != null) logsFormatLabel.Text = LocalizationManager.Get("SettingsPage_Section_Logs_Format");

        var statePathLabel = this.FindControl<TextBlock>("StatePathLabel");
        if (statePathLabel != null) statePathLabel.Text = LocalizationManager.Get("SettingsPage_Section_State_Path");

        var extensionsLabel = this.FindControl<TextBlock>("ExtensionsLabel");
        if (extensionsLabel != null) extensionsLabel.Text = LocalizationManager.Get("SettingsPage_Section_Encryption_Extensions");

        var appsLabel = this.FindControl<TextBlock>("AppsLabel");
        if (appsLabel != null) appsLabel.Text = LocalizationManager.Get("SettingsPage_Section_BusinessApps_List");

        var currentLanguageLabel = this.FindControl<TextBlock>("CurrentLanguageLabel");
        if (currentLanguageLabel != null) currentLanguageLabel.Text = LocalizationManager.Get("SettingsPage_Section_Language_Current");

        var aboutSectionTitle = this.FindControl<TextBlock>("AboutSectionTitle");
        if (aboutSectionTitle != null) aboutSectionTitle.Text = LocalizationManager.Get("SettingsPage_Section_About");

        var versionLabel = this.FindControl<TextBlock>("VersionLabel");
        if (versionLabel != null) versionLabel.Text = LocalizationManager.Get("SettingsPage_Section_About_Version");

        UpdateCurrentLanguageDisplay();

        PopulateData();
    }

    private void UpdateCurrentLanguageDisplay()
    {
        var currentLanguageText = this.FindControl<TextBlock>("CurrentLanguageText");
        if (currentLanguageText != null)
        {
            string languageName = LocalizationManager.CurrentCulture.TwoLetterISOLanguageName == "fr"
                ? LocalizationManager.Get("Language_French")
                : LocalizationManager.Get("Language_English");
            currentLanguageText.Text = languageName;
        }
    }

    private void PopulateData()
    {
        try
        {
            UpdateCurrentLanguageDisplay();

            var logsPathText = this.FindControl<TextBlock>("LogsPathText");
            if (logsPathText != null)
            {
                string logsPath = _configParser.GetLogsPath();
                logsPathText.Text = Path.GetFullPath(logsPath);
            }

            var logsFormatText = this.FindControl<TextBlock>("LogsFormatText");
            if (logsFormatText != null) logsFormatText.Text = _configParser.GetLogFormat().ToUpper();

            var statePathText = this.FindControl<TextBlock>("StatePathText");
            if (statePathText != null)
            {
                var stateFilePath = _configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(stateFilePath))
                {
                    statePathText.Text = Path.GetFullPath(stateFilePath);
                }
                else
                {
                    statePathText.Text = "N/A";
                }
            }

            var versionText = this.FindControl<TextBlock>("VersionText");
            if (versionText != null)
            {
                string version = GetApplicationVersion();
                versionText.Text = version;
            }

            var extensionsPanel = this.FindControl<WrapPanel>("ExtensionsPanel");
            if (extensionsPanel != null)
            {
                extensionsPanel.Children.Clear();
                var extensions = _configParser.GetEncryptionExtensions();

                if (extensions.Count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = LocalizationManager.Get("SettingsPage_NoExtensions"),
                        Foreground = Avalonia.Media.Brushes.Gray,
                        FontSize = 12
                    };
                    extensionsPanel.Children.Add(emptyText);
                }
                else
                {
                    foreach (var ext in extensions)
                    {
                        var badge = CreateBadge(ext);
                        extensionsPanel.Children.Add(badge);
                    }
                }
            }

            var appsPanel = this.FindControl<WrapPanel>("AppsPanel");
            if (appsPanel != null)
            {
                appsPanel.Children.Clear();
                var apps = _configParser.GetBusinessApplications();

                if (apps.Count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = LocalizationManager.Get("SettingsPage_NoApps"),
                        Foreground = Avalonia.Media.Brushes.Gray,
                        FontSize = 12
                    };
                    appsPanel.Children.Add(emptyText);
                }
                else
                {
                    foreach (var app in apps)
                    {
                        var badge = CreateBadge(app);
                        appsPanel.Children.Add(badge);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error populating settings data: {ex.Message}");
        }
    }

    private Border CreateBadge(string text)
    {
        return new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F3F4F6")),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(8, 3),
            Margin = new Avalonia.Thickness(4, 4, 4, 4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#374151"))
            }
        };
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
}
