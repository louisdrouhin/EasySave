using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;
using EasySave.Models;

namespace EasySave.Gui.ViewModels;

// ViewModel pour la page Jobs
// Gère la liste des jobs, la sélection multiple et les opérations sur jobs
public class JobsPageViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private bool _isSelectionBarVisible;
    private string _selectionCountText = "";

    // Initialise la page des jobs
    // Charge les jobs existants et s'abonne aux événements du Core
    // @param jobManager - gestionnaire central des jobs
    public JobsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

        // Collections
        Jobs = new ObservableCollection<JobViewModel>();

        // Commandes
        CreateJobCommand = new RelayCommand(_ => OnCreateJob());
        DeselectAllCommand = new RelayCommand(_ => OnDeselectAll());
        RunSelectedCommand = new RelayCommand(_ => OnRunSelected());

        // Charge les jobs depuis le Core
        LoadJobs();

        // S'abonne aux événements du Core
        _jobManager.JobStateChanged += OnJobStateChanged;
        _jobManager.JobCreated += OnJobCreated;
        _jobManager.JobRemoved += OnJobRemoved;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    // Collection observable des jobs pour la liaison aux contrôles
    public ObservableCollection<JobViewModel> Jobs { get; }

    // True si la barre de sélection (déselect all, run selected) est visible
    public bool IsSelectionBarVisible
    {
        get => _isSelectionBarVisible;
        set => SetProperty(ref _isSelectionBarVisible, value);
    }

    // Texte indiquant le nombre de jobs sélectionnés (ex: "2 jobs selected")
    public string SelectionCountText
    {
        get => _selectionCountText;
        set => SetProperty(ref _selectionCountText, value);
    }

    // Titres et labels traduits
    public string HeaderTitle => LocalizationManager.Get("JobsPage_Header_Title") ?? "Jobs";
    public string HeaderSubtitle => LocalizationManager.Get("JobsPage_Header_Subtitle") ?? "";
    public string CreateJobLabel => LocalizationManager.Get("JobsPage_Button_NewJob") ?? "New Job";
    public string DeselectAllLabel => LocalizationManager.Get("JobsPage_DeselectAll") ?? "Deselect All";
    // Label avec nombre de jobs sélectionnés
    public string RunSelectedLabel
    {
        get
        {
            var selectedCount = Jobs.Count(j => j.IsSelected);
            return LocalizationManager.GetFormatted("JobsPage_RunSelected", selectedCount) ?? $"Run Selected ({selectedCount})";
        }
    }


    // Commande pour créer un nouveau job
    public ICommand CreateJobCommand { get; }

    // Commande pour désélectionner tous les jobs
    public ICommand DeselectAllCommand { get; }

    // Commande pour exécuter tous les jobs sélectionnés
    public ICommand RunSelectedCommand { get; }


    public event EventHandler? CreateJobRequested;
    public event EventHandler<JobViewModel>? PlayJobRequested;
    public event EventHandler? RunSelectedRequested;
    public event EventHandler<string>? ErrorOccurred;


    // Charge les jobs depuis le JobManager et les ajoute à la collection observable
    // S'abonne également aux événements de chaque JobViewModel pour les actions (play, pause, delete, etc.)
    private void LoadJobs()
    {
        var jobs = _jobManager.GetJobs();
        foreach (var job in jobs)
        {
            var vm = new JobViewModel(job);
            SubscribeToJobVM(vm);
            Jobs.Add(vm);
        }
    }

    // Abonne les événements d'un JobViewModel pour gérer les actions demandées par l'utilisateur (play, pause, delete, etc.)
    // @param vm - le JobViewModel à abonner
    private void SubscribeToJobVM(JobViewModel vm)
    {
        vm.PlayRequested += OnJobPlayRequested;
        vm.PauseRequested += OnJobPauseRequested;
        vm.ResumeRequested += OnJobResumeRequested;
        vm.StopRequested += OnJobStopRequested;
        vm.DeleteRequested += OnJobDeleteRequested;
        vm.SelectionChanged += (s, e) => UpdateSelectionBar();
    }

    // Déclenche l'événement CreateJobRequested pour signaler que l'utilisateur souhaite créer un nouveau job
    private void OnCreateJob()
    {
        CreateJobRequested?.Invoke(this, EventArgs.Empty);
    }

    // Appelé lorsque le formulaire de création de job est soumis. Tente de créer un nouveau job via le JobManager et gère les erreurs éventuelles.
    public void OnJobCreated(string name, JobType type, string sourcePath, string destinationPath)
    {
        try
        {
            _jobManager.CreateJob(name, type, sourcePath, destinationPath);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Désélectionne tous les jobs et met à jour la barre de sélection
    private void OnDeselectAll()
    {
        foreach (var job in Jobs)
        {
            job.IsSelected = false;
        }
        UpdateSelectionBar();
    }

    // Déclenche l'événement RunSelectedRequested pour signaler que l'utilisateur souhaite exécuter les jobs sélectionnés
    private void OnRunSelected()
    {
        RunSelectedRequested?.Invoke(this, EventArgs.Empty);
    }

    // Exécute un job donné en arrière-plan et gère les erreurs éventuelles. Deselect le job après lancement pour une meilleure UX.
    // @param vm - le JobViewModel du job à exécuter
    // @param password - mot de passe à utiliser pour le job (si nécessaire)
    public async Task ExecutePlayJob(JobViewModel vm, string password)
    {
        try
        {
            _ = Task.Run(() =>
            {
                _jobManager.LaunchJob(vm.Job, password);
            });

            vm.IsSelected = false;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Exécute tous les jobs sélectionnés en arrière-plan et gère les erreurs éventuelles. Deselect tous les jobs après lancement
    // @param selected - liste des JobViewModel sélectionnés à exécuter
    // @param password - mot de passe à utiliser pour les jobs (si nécessaire)
    public async Task ExecuteRunSelected(List<JobViewModel> selected, string password)
    {
        try
        {
            foreach (var vm in selected)
            {
                await ExecutePlayJob(vm, password);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Vérifie que les applications métier sont lancé
    public string? CheckBusinessApp()
    {
        return _jobManager.CheckBusinessApplications();
    }

    // Récupère la liste des jobs sélectionnés
    // @returns liste des JobViewModel actuellement sélectionnés
    public List<JobViewModel> GetSelectedJobs()
    {
        return Jobs.Where(j => j.IsSelected).ToList();
    }

    // Met à jour la visibilité de la barre de sélection et le texte indiquant le nombre de jobs sélectionnés
    // Appelé lorsque la sélection d'un job change pour refléter l'état actuel de la sélection
    private void UpdateSelectionBar()
    {
        var selectedCount = Jobs.Count(j => j.IsSelected);
        IsSelectionBarVisible = selectedCount > 0;
        SelectionCountText = selectedCount > 0 ? $"{selectedCount} selected" : "";
        OnPropertyChanged(nameof(RunSelectedLabel));
    }

    // Gère la demande de lancement d'un job en déclenchant l'événement PlayJobRequested avec le JobViewModel concerné
    // @param sender - l'objet qui a déclenché l'événement (généralement un bouton dans le JobViewModel)
    // @param vm - le JobViewModel du job à lancer
    private void OnJobPlayRequested(object? sender, JobViewModel vm)
    {
        PlayJobRequested?.Invoke(this, vm);
    }

    // Gère la demande de pause d'un job en appelant la méthode PauseJob du JobManager et en gérant les erreurs éventuelles
    // @param sender - l'objet qui a déclenché l'événement (généralement un bouton dans le JobViewModel)
    // @param vm - le JobViewModel du job à mettre en pause
    private void OnJobPauseRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.PauseJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Gère la demande de reprise d'un job en appelant la méthode ResumeJob du JobManager et en gérant les erreurs éventuelles
    // @param sender - l'objet qui a déclenché l'événement (généralement un bouton dans le JobViewModel)
    // @param vm - le JobViewModel du job à reprendre
    private void OnJobResumeRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.ResumeJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Gère la demande d'arrêt d'un job en appelant la méthode StopJob du JobManager et en gérant les erreurs éventuelles
    // @param sender - l'objet qui a déclenché l'événement (généralement un bouton dans le JobViewModel)
    // @param vm - le JobViewModel du job à arrêter
    private void OnJobStopRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.StopJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Gère la demande de suppression d'un job en appelant la méthode removeJob du JobManager et en gérant les erreurs éventuelles
    // @param sender - l'objet qui a déclenché l'événement (généralement un bouton dans le JobViewModel)
    // @param vm - le JobViewModel du job à supprimer
    private void OnJobDeleteRequested(object? sender, JobViewModel vm)
    {
        var index = Jobs.IndexOf(vm);
        if (index >= 0)
        {
            try
            {
                _jobManager.removeJob(index);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }
    }

    // Gère les changements d'état d'un job en appliquant les nouvelles informations au JobViewModel correspondant
    // Utilise Dispatcher.Invoke pour s'assurer que les mises à jour de l'interface utilisateur sont effectuées
    // @param sender - l'objet qui a déclenché l'événement (généralement le JobManager)
    // @param entry - l'objet StateEntry contenant les informations sur le changement d'état du job

    private void OnJobStateChanged(object? sender, StateEntry entry)
    {
        System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Received event: {entry.JobName} = {entry.State}");

        var jobVM = Jobs.FirstOrDefault(j => j.Name == entry.JobName);
        if (jobVM != null)
        {
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Applying state for {entry.JobName}");
            Dispatcher.UIThread.Invoke(() => jobVM.ApplyState(entry), DispatcherPriority.Default);
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] State applied for {entry.JobName}, current state: {jobVM.State}");
        }
    }

    // Gère la création d'un nouveau job en ajoutant un nouveau JobViewModel à la collection observable et en s'abonnant à ses événements
    // Utilise Dispatcher.Invoke pour s'assurer que les mises à jour de l'interface utilisateur sont effectuées
    // @param sender - l'objet qui a déclenché l'événement (généralement le JobManager)
    // @param job - le Job nouvellement créé à ajouter à la liste
    private void OnJobCreated(object? sender, Job job)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new JobViewModel(job);
            SubscribeToJobVM(vm);
            Jobs.Add(vm);
        });
    }

    // Gère la suppression d'un job en retirant le JobViewModel correspondant de la collection observable
    // @param sender - l'objet qui a déclenché l'événement (généralement le JobManager)
    // @param index - l'index du job supprimé dans la collection
    private void OnJobRemoved(object? sender, int index)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (index >= 0 && index < Jobs.Count)
            {
                Jobs.RemoveAt(index);
            }
            UpdateSelectionBar();
        });
    }

    // Gère les changements de langue en déclenchant les notifications de changement de propriété pour tous les éléments
    // @param sender - l'objet qui a déclenché l'événement (généralement le LocalizationManager)
    // @param e - les arguments de l'événement contenant les informations sur la langue sélectionnée
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CreateJobLabel));
        OnPropertyChanged(nameof(DeselectAllLabel));
        OnPropertyChanged(nameof(RunSelectedLabel));
    }


    // Se désabonne de tous les événements pour éviter les fuites de mémoire lorsque la page est détruite
    public void Dispose()
    {
        _jobManager.JobStateChanged -= OnJobStateChanged;
        _jobManager.JobCreated -= OnJobCreated;
        _jobManager.JobRemoved -= OnJobRemoved;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;

        foreach (var job in Jobs)
        {
            job.PlayRequested -= OnJobPlayRequested;
            job.PauseRequested -= OnJobPauseRequested;
            job.ResumeRequested -= OnJobResumeRequested;
            job.StopRequested -= OnJobStopRequested;
            job.DeleteRequested -= OnJobDeleteRequested;
        }
    }
}
