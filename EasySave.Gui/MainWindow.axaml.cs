using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.GUI.Pages;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    private readonly JobsPage _jobsPage = new();
    private readonly LogsPage _logsPage = new();
    private readonly StatePage _statePage = new();
    private readonly SettingsPage _settingsPage = new();

    public MainWindow()
    {
        InitializeComponent();
        PageHost.Content = _jobsPage;
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