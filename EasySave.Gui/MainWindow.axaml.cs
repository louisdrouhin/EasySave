using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using EasySave.GUI.Pages;
using EasySave.Core;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    private readonly JobManager _jobManager = null!;
    private readonly JobsPage _jobsPage = null!;
    private readonly LogsPage _logsPage = null!;
    private readonly StatePage _statePage = null!;
    private readonly SettingsPage _settingsPage = null!;

    public MainWindow()
    {
        InitializeComponent();

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
            _logsPage = new LogsPage();
            _statePage = new StatePage();
            _settingsPage = new SettingsPage();
            Console.WriteLine("Pages created successfully");

            PageHost.Content = _jobsPage;
            Console.WriteLine("MainWindow initialized successfully");
            System.Diagnostics.Debug.WriteLine("MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"FATAL ERROR in MainWindow:\n{ex.Message}\n\nStack:\n{ex.StackTrace}";
            Console.WriteLine(errorMsg);
            System.Diagnostics.Debug.WriteLine(errorMsg);

            // Show error in UI
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
    }

    private void OnLogsClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _logsPage;
    }

    private void OnStateClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _statePage;
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        PageHost.Content = _settingsPage;
    }
}