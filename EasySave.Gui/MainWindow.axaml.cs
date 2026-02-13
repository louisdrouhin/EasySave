using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using EasySave.GUI.Pages;
using EasySave.Core;
using EasySave.Core.Localization;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly JobsPage _jobsPage = null!;
    private readonly LogsPage _logsPage = null!;
    private readonly SettingsPage _settingsPage = null!;

    public MainWindow()
    {
        InitializeComponent();

        Title = LocalizationManager.Get("MainWindow_Title");
        JobsButton.Content = LocalizationManager.Get("MainWindow_Menu_Jobs");
        LogsButton.Content = LocalizationManager.Get("MainWindow_Menu_Logs");
        SettingsButton.Content = LocalizationManager.Get("MainWindow_Menu_Settings");

        try
        {
            Console.WriteLine("Starting MainWindow initialization...");
            System.Diagnostics.Debug.WriteLine("Starting MainWindow initialization...");

            Console.WriteLine("Creating JobManager...");
            _jobManager = new JobManager();
            Console.WriteLine("JobManager created successfully");
            System.Diagnostics.Debug.WriteLine("JobManager initialized successfully");

            Console.WriteLine("Creating pages...");

            _jobsPage = new JobsPage(_jobManager);
            _logsPage = new LogsPage(_jobManager.ConfigParser);
            _settingsPage = new SettingsPage();

            Console.WriteLine("Pages created successfully");

            PageHost.Content = _jobsPage;
            Console.WriteLine("MainWindow initialized successfully");
            System.Diagnostics.Debug.WriteLine("MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = LocalizationManager.GetFormatted("MainWindow_FatalError", ex.Message, ex.StackTrace ?? "");
            Console.WriteLine(errorMsg);
            System.Diagnostics.Debug.WriteLine(errorMsg);

            PageHost.Content = new TextBlock
            {
                Text = errorMsg,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(20)
            };
        }
    }

    private void OnJobsClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _jobsPage;
        UpdateMenuButtonStyles("Jobs");
    }

    private void OnLogsClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _logsPage;
        UpdateMenuButtonStyles("Logs");
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _settingsPage;
        UpdateMenuButtonStyles("Settings");
    }

    private void UpdateMenuButtonStyles(string activePage)
    {
        // Réinitialiser tous les boutons en inactif
        JobsButton.Classes.Remove("menu-button-active");
        LogsButton.Classes.Remove("menu-button-active");
        SettingsButton.Classes.Remove("menu-button-active");

        JobsButton.Classes.Add("menu-button-inactive");
        LogsButton.Classes.Add("menu-button-inactive");
        SettingsButton.Classes.Add("menu-button-inactive");

        // Activer le bouton sélectionné
        switch (activePage)
        {
            case "Jobs":
                JobsButton.Classes.Remove("menu-button-inactive");
                JobsButton.Classes.Add("menu-button-active");
                break;
            case "Logs":
                LogsButton.Classes.Remove("menu-button-inactive");
                LogsButton.Classes.Add("menu-button-active");
                break;
            case "Settings":
                SettingsButton.Classes.Remove("menu-button-inactive");
                SettingsButton.Classes.Add("menu-button-active");
                break;
        }
    }
}
