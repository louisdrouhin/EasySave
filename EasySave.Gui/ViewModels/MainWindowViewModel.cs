using System;
using System.Windows.Input;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel for the main window
// Manages page navigation and creates ViewModels for pages
public class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private ViewModelBase _currentPage;

    // Initializes the ViewModel and pages
    // Creates JobManager and initializes the 3 pages (Jobs, Logs, Settings)
    public MainWindowViewModel()
    {
        _jobManager = new JobManager();

        JobsPageVm = new JobsPageViewModel(_jobManager);
        LogsPageVm = new LogsPageViewModel(_jobManager);
        SettingsPageVm = new SettingsPageViewModel(_jobManager);

        _currentPage = JobsPageVm;

        NavigateToJobsCommand = new RelayCommand(_ => NavigateTo(JobsPageVm));
        NavigateToLogsCommand = new RelayCommand(_ => NavigateTo(LogsPageVm));
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateTo(SettingsPageVm));

        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    // ViewModels for pages accessible from MainWindow
    public JobsPageViewModel JobsPageVm { get; }

    // ViewModel for the Logs page, manages log display and associated interactions
    public LogsPageViewModel LogsPageVm { get; }

    // ViewModel for the Settings page, manages application settings and associated interactions
    public SettingsPageViewModel SettingsPageVm { get; }


    // Page currently displayed in the MainWindow
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    // Indicates if the Jobs page is active
    public bool IsJobsActive => CurrentPage is JobsPageViewModel;

    // Indicates if the Logs page is active
    public bool IsLogsActive => CurrentPage is LogsPageViewModel;

    // Indicates if the Settings page is active
    public bool IsSettingsActive => CurrentPage is SettingsPageViewModel;

    // Title of the main window
    public string WindowTitle => LocalizationManager.Get("MainWindow_Title") ?? "EasySave";

    // Label for the Jobs button
    public string JobsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Jobs") ?? "Jobs";

    // Label for the Logs button
    public string LogsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Logs") ?? "Logs";

    // Label for the Settings button
    public string SettingsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Settings") ?? "Settings";

    // Command to navigate to the Jobs page
    public ICommand NavigateToJobsCommand { get; }

    // Command to navigate to the Logs page
    public ICommand NavigateToLogsCommand { get; }

    // Command to navigate to the Settings page
    public ICommand NavigateToSettingsCommand { get; }


    // Changes the active page and updates IsXxxActive properties
    // @param page - page to navigate to
    private void NavigateTo(ViewModelBase page)
    {
        if (CurrentPage == page)
            return;

        CurrentPage = page;

        OnPropertyChanged(nameof(IsJobsActive));
        OnPropertyChanged(nameof(IsLogsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    // Handles language changes and updates labels
    // @param sender - event source
    // @param e - arguments containing language code and CultureInfo
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(JobsButtonLabel));
        OnPropertyChanged(nameof(LogsButtonLabel));
        OnPropertyChanged(nameof(SettingsButtonLabel));
    }


    // Cleans up resources: unsubscribes from events and closes JobManager
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        JobsPageVm.Dispose();
        LogsPageVm.Dispose();
        SettingsPageVm.Dispose();
        _jobManager.Close();
    }
}
