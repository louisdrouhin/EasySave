using System;
using System.Windows.Input;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

/// <summary>
/// ViewModel for the main application window - handles navigation and page management
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        _jobManager = new JobManager();

        // Create page ViewModels
        JobsPageVm = new JobsPageViewModel(_jobManager);
        LogsPageVm = new LogsPageViewModel(_jobManager);
        SettingsPageVm = new SettingsPageViewModel(_jobManager);

        // Set initial page
        _currentPage = JobsPageVm;

        // Navigation commands
        NavigateToJobsCommand = new RelayCommand(_ => NavigateTo(JobsPageVm));
        NavigateToLogsCommand = new RelayCommand(_ => NavigateTo(LogsPageVm));
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateTo(SettingsPageVm));

        // Subscribe to language changes
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    #region Page ViewModels

    public JobsPageViewModel JobsPageVm { get; }
    public LogsPageViewModel LogsPageVm { get; }
    public SettingsPageViewModel SettingsPageVm { get; }

    #endregion

    #region Current Page

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public bool IsJobsActive => CurrentPage is JobsPageViewModel;
    public bool IsLogsActive => CurrentPage is LogsPageViewModel;
    public bool IsSettingsActive => CurrentPage is SettingsPageViewModel;

    #endregion

    #region Localized Labels

    public string WindowTitle => LocalizationManager.Get("MainWindow_Title") ?? "EasySave";
    public string JobsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Jobs") ?? "Jobs";
    public string LogsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Logs") ?? "Logs";
    public string SettingsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Settings") ?? "Settings";

    #endregion

    #region Commands

    public ICommand NavigateToJobsCommand { get; }
    public ICommand NavigateToLogsCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }

    #endregion

    #region Methods

    private void NavigateTo(ViewModelBase page)
    {
        if (CurrentPage == page)
            return;

        CurrentPage = page;

        // Update active state properties
        OnPropertyChanged(nameof(IsJobsActive));
        OnPropertyChanged(nameof(IsLogsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(JobsButtonLabel));
        OnPropertyChanged(nameof(LogsButtonLabel));
        OnPropertyChanged(nameof(SettingsButtonLabel));
    }

    #endregion

    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        JobsPageVm.Dispose();
        LogsPageVm.Dispose();
        SettingsPageVm.Dispose();
        _jobManager.Close();
    }
}
