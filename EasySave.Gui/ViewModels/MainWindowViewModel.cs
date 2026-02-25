using System;
using System.Windows.Input;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel de la fenêtre principale
// Gère la navigation entre pages et crée les ViewModels des pages
public class MainWindowViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private ViewModelBase _currentPage;

    // Initialise le ViewModel et les pages
    // Crée JobManager et initialise les 3 pages (Jobs, Logs, Settings)
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

    // ViewModels des pages accessibles depuis la MainWindow
    public JobsPageViewModel JobsPageVm { get; }

    // ViewModel de la page Logs, gère l'affichage des logs et les interactions associées
    public LogsPageViewModel LogsPageVm { get; }

    // ViewModel de la page Settings, gère les paramètres de l'application et les interactions associées
    public SettingsPageViewModel SettingsPageVm { get; }


    // Page actuellement affichée dans la MainWindow
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    // Indique si la page Jobs est active
    public bool IsJobsActive => CurrentPage is JobsPageViewModel;

    // Indique si la page Logs est active
    public bool IsLogsActive => CurrentPage is LogsPageViewModel;

    // Indique si la page Settings est active
    public bool IsSettingsActive => CurrentPage is SettingsPageViewModel;

    // Titre de la fenêtre principale
    public string WindowTitle => LocalizationManager.Get("MainWindow_Title") ?? "EasySave";

    // Label du bouton Jobs
    public string JobsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Jobs") ?? "Jobs";

    // Label du bouton Logs
    public string LogsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Logs") ?? "Logs";

    // Label du bouton Settings
    public string SettingsButtonLabel => LocalizationManager.Get("MainWindow_Menu_Settings") ?? "Settings";

    // Commande pour naviguer vers la page Jobs
    public ICommand NavigateToJobsCommand { get; }

    // Commande pour naviguer vers la page Logs
    public ICommand NavigateToLogsCommand { get; }

    // Commande pour naviguer vers la page Settings
    public ICommand NavigateToSettingsCommand { get; }


    // Change la page active et met à jour les propriétés IsXxxActive
    // @param page - page vers laquelle naviguer
    private void NavigateTo(ViewModelBase page)
    {
        if (CurrentPage == page)
            return;

        CurrentPage = page;

        OnPropertyChanged(nameof(IsJobsActive));
        OnPropertyChanged(nameof(IsLogsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    // Gère les changements de langue et actualise les labels
    // @param sender - source de l'événement
    // @param e - arguments contenant le code langue et CultureInfo
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(JobsButtonLabel));
        OnPropertyChanged(nameof(LogsButtonLabel));
        OnPropertyChanged(nameof(SettingsButtonLabel));
    }


    // Nettoie les ressources: se désabonne des événements et ferme JobManager
    public void Dispose()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        JobsPageVm.Dispose();
        LogsPageVm.Dispose();
        SettingsPageVm.Dispose();
        _jobManager.Close();
    }
}
