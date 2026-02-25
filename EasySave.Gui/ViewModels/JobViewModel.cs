using System;
using System.Windows.Input;
using Avalonia.Media;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;
using EasySave.Models;

namespace EasySave.Gui.ViewModels;

// ViewModel pour une tâche de sauvegarde individuelle
// Gère l'état, la progression et les interactions utilisateur (play/pause/stop/delete)
public class JobViewModel : ViewModelBase
{
    private readonly Job _job;
    private JobState _state = JobState.Inactive;
    private JobState _lastAppliedState = JobState.Inactive;
    private DateTime _lastStateChangeTime = DateTime.MinValue;
    private double _progress;
    private int? _totalFiles;
    private int? _remainingFiles;
    private long? _totalSize;
    private long? _remainingSize;
    private bool _isSelected;
    private bool _isExpanded;
    private bool _isDeleteConfirming;

    // Accès au modèle Job sous-jacent
    public Job Job => _job;

    // Initialise le ViewModel pour un job donné
    // @param job - le job à gérer
    public JobViewModel(Job job)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));

        // Commandes utilisateur
        PlayPauseResumeCommand = new RelayCommand(_ => OnPlayPauseResume());
        StopCommand = new RelayCommand(_ => OnStop());
        DeleteCommand = new RelayCommand(_ => OnDelete());
        ToggleExpandCommand = new RelayCommand(_ => OnToggleExpand());
        ConfirmDeleteCommand = new RelayCommand(_ => OnConfirmDelete());
        CancelDeleteCommand = new RelayCommand(_ => OnCancelDelete());

        // S'abonne aux changements de langue pour actualiser le label d'état
        LocalizationManager.LanguageChanged += (s, e) => OnPropertyChanged(nameof(StatusLabel));
    }


    // Nom du job
    public string Name => _job.Name;

    // Chemin du répertoire source
    public string SourcePath => _job.SourcePath;

    // Chemin du répertoire destination
    public string DestinationPath => _job.DestinationPath;

    // Label du type de sauvegarde (FULL ou DIFF)
    public string TypeLabel
    {
        get
        {
            return _job.Type switch
            {
                JobType.Full => "FULL",
                JobType.Differential => "DIFF",
                _ => _job.Type.ToString().ToUpper()
            };
        }
    }


    // État actuel du job (Active, Inactive, Paused)
    public JobState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    // True si le job est en cours d'exécution
    public bool IsActive => _state == JobState.Active;

    // True si le job est suspendu
    public bool IsPaused => _state == JobState.Paused;

    // True si le job est arrêté ou jamais lancé
    public bool IsInactive => _state == JobState.Inactive;


    // Label d'état traduit du job (Active, Paused, Inactive)
    public string StatusLabel
    {
        get
        {
            return _state switch
            {
                JobState.Active => LocalizationManager.Get("JobCard_Status_Active") ?? "Active",
                JobState.Paused => LocalizationManager.Get("JobCard_Status_Paused") ?? "Paused",
                JobState.Inactive => LocalizationManager.Get("JobCard_Status_Inactive") ?? "Inactive",
                _ => "Unknown"
            };
        }
    }

    // Couleur du badge d'état (Vert=Active, Orange=Paused, Gris=Inactive)
    public IBrush StatusBadgeColor
    {
        get
        {
            return _state switch
            {
                JobState.Active => new SolidColorBrush(Color.Parse("#22C55E")), // Green
                JobState.Paused => new SolidColorBrush(Color.Parse("#F59E0B")), // Amber
                JobState.Inactive => new SolidColorBrush(Color.Parse("#6B7280")), // Gray
                _ => new SolidColorBrush(Color.Parse("#9CA3AF"))
            };
        }
    }


    // Pourcentage de progression (0-100)
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    // Nombre total de fichiers à traiter
    public int? TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    // Nombre de fichiers restants à traiter
    public int? RemainingFiles
    {
        get => _remainingFiles;
        set => SetProperty(ref _remainingFiles, value);
    }

    // Taille totale à transférer en bytes
    public long? TotalSize
    {
        get => _totalSize;
        set => SetProperty(ref _totalSize, value);
    }

    // Taille restante à transférer en bytes
    public long? RemainingSize
    {
        get => _remainingSize;
        set => SetProperty(ref _remainingSize, value);
    }

    // Texte formaté des fichiers restants (ex: "5/10 files")
    public string RemainingFilesText
    {
        get
        {
            if (RemainingFiles == null || TotalFiles == null)
                return "";
            return $"{RemainingFiles}/{TotalFiles} files";
        }
    }

    // Texte du pourcentage formaté (ex: "45.5%")
    public string ProgressText
    {
        get => $"{Progress:F1}%";
    }

    // Texte de la taille restante formatée en MB (ex: "12.50 MB")
    public string RemainingSizeText
    {
        get
        {
            if (RemainingSize == null)
                return "0.00 MB";
            return $"{RemainingSize:F2} MB";
        }
    }

    // True si la progression doit être affichée (Active ou Paused)
    public bool IsProgressVisible => IsActive || IsPaused;


    // Indique si le job est sélectionné
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                SetProperty(ref _isSelected, value);
                SelectionChanged?.Invoke(this, (this, _isSelected));
            }
        }
    }

    // Indique si la carte du job est expandue pour voir plus de détails
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    // True si le dialogue de confirmation de suppression est affiché
    public bool IsDeleteConfirming
    {
        get => _isDeleteConfirming;
        set => SetProperty(ref _isDeleteConfirming, value);
    }

    // Données géométriques pour l'icône du bouton Play/Pause/Resume
    // Play (inactif), Pause (actif), Flèche (reprise)
    public Geometry PlayPauseIconData
    {
        get
        {
            if (IsActive)
                return Geometry.Parse("M14,19H18V5H14M6,19H10V5H6V19Z"); // Pause
            else if (IsPaused)
                return Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"); // Arrow right (Resume)
            else
                return Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"); // Play
        }
    }

    // Données géométriques pour l'icône du bouton Supprimer
    // Stop (en cours/pause), Trash (inactif)
    public Geometry DeleteIconData
    {
        get
        {
            if (IsActive || IsPaused)
                return Geometry.Parse("M18,18H6V6H18V18Z"); // Stop icon
            else
                return Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z"); // Trash icon
        }
    }

    // Couleur de fond du bouton Play (Vert=inactif, Orange=actif/pause)
    public IBrush PlayButtonBackground
    {
        get
        {
            if (IsInactive)
                return new SolidColorBrush(Color.Parse("#22C55E")); // Green
            else
                return new SolidColorBrush(Color.Parse("#F97316")); // Orange
        }
    }

    // Couleur du bouton Play
    public IBrush PlayButtonColor => IsInactive ? Brushes.Green : Brushes.Orange;



    // Commande Play/Pause/Resume selon l'état
    public ICommand PlayPauseResumeCommand { get; }

    // Commande pour arrêter le job
    public ICommand StopCommand { get; }

    // Commande pour supprimer le job
    public ICommand DeleteCommand { get; }

    // Commande pour montrer/cacher les détails du job
    public ICommand ToggleExpandCommand { get; }

    // Commande pour confirmer la suppression
    public ICommand ConfirmDeleteCommand { get; }

    // Commande pour annuler la suppression
    public ICommand CancelDeleteCommand { get; }


    // Déclenché quand l'utilisateur clique sur Play
    public event EventHandler<JobViewModel>? PlayRequested;

    // Déclenché quand l'utilisateur clique sur Pause
    public event EventHandler<JobViewModel>? PauseRequested;

    // Déclenché quand l'utilisateur clique sur Resume
    public event EventHandler<JobViewModel>? ResumeRequested;

    // Déclenché quand l'utilisateur clique sur Stop
    public event EventHandler<JobViewModel>? StopRequested;

    // Déclenché quand l'utilisateur confirme la suppression
    public event EventHandler<JobViewModel>? DeleteRequested;

    // Déclenché quand la sélection du job change
    public event EventHandler<(JobViewModel, bool)>? SelectionChanged;


    // Applique une mise à jour d'état depuis la couche Core
    // Met à jour l'état et la progression du job
    // @param entry - StateEntry du Core contenant les infos à jour
    public void ApplyState(StateEntry entry)
    {
        if (entry.JobName != _job.Name)
            return;

        var oldState = _state;
        System.Diagnostics.Debug.WriteLine($"[{_job.Name}] ApplyState called: {oldState} → {entry.State}");

        State = entry.State;
        System.Diagnostics.Debug.WriteLine($"[{_job.Name}] State applied: {_state}");

        Progress = entry.Progress ?? 0;
        TotalFiles = entry.TotalFiles;
        RemainingFiles = entry.RemainingFiles;
        TotalSize = entry.TotalSizeToTransfer;
        RemainingSize = entry.RemainingSizeToTransfer;

        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsInactive));
        OnPropertyChanged(nameof(IsProgressVisible));
        OnPropertyChanged(nameof(RemainingFilesText));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(RemainingSizeText));
        OnPropertyChanged(nameof(PlayPauseIconData));
        OnPropertyChanged(nameof(DeleteIconData));
        OnPropertyChanged(nameof(PlayButtonColor));
        OnPropertyChanged(nameof(PlayButtonBackground));
        OnPropertyChanged(nameof(StatusBadgeColor));
        OnPropertyChanged(nameof(StatusLabel));
    }

    // Gère le clic sur le bouton Play/Pause/Resume
    // Change l'état selon l'état actuel et déclenche l'événement approprié
    private void OnPlayPauseResume()
    {
        switch (_state)
        {
            case JobState.Inactive:
                PlayRequested?.Invoke(this, this);
                break;
            case JobState.Active:
                PauseRequested?.Invoke(this, this);
                break;
            case JobState.Paused:
                ResumeRequested?.Invoke(this, this);
                break;
        }
    }

    // Gère le clic sur le bouton Stop
    private void OnStop()
    {
        StopRequested?.Invoke(this, this);
    }

    // Gère le clic sur le bouton Delete
    // Si en cours: arrête le job
    // Si inactif: affiche la confirmation de suppression
    private void OnDelete()
    {
        if (IsActive || IsPaused)
        {
            // Si en cours, arrête d'abord
            StopRequested?.Invoke(this, this);
        }
        else
        {
            // Si inactif, affiche la confirmation
            IsDeleteConfirming = !IsDeleteConfirming;
        }
    }

    // Gère la confirmation de suppression
    // Ferme le dialogue et déclenche l'événement DeleteRequested
    private void OnConfirmDelete()
    {
        IsDeleteConfirming = false;
        DeleteRequested?.Invoke(this, this);
    }

    // Gère l'annulation de la suppression
    // Ferme simplement le dialogue
    private void OnCancelDelete()
    {
        IsDeleteConfirming = false;
    }

    // Bascule l'affichage des détails du job
    private void OnToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}
