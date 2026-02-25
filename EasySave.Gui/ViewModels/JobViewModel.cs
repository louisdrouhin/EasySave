using System;
using System.Windows.Input;
using Avalonia.Media;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;
using EasySave.Models;

namespace EasySave.Gui.ViewModels;

// ViewModel for an individual backup job
// Manages state, progress, and user interactions (play/pause/stop/delete)
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

    // Access to the underlying Job model
    public Job Job => _job;

    // Initializes the ViewModel for a given job
    // @param job - the job to manage
    public JobViewModel(Job job)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));

        // User commands
        PlayPauseResumeCommand = new RelayCommand(_ => OnPlayPauseResume());
        StopCommand = new RelayCommand(_ => OnStop());
        DeleteCommand = new RelayCommand(_ => OnDelete());
        ToggleExpandCommand = new RelayCommand(_ => OnToggleExpand());
        ConfirmDeleteCommand = new RelayCommand(_ => OnConfirmDelete());
        CancelDeleteCommand = new RelayCommand(_ => OnCancelDelete());

        // Subscribes to language changes to update the status label
        LocalizationManager.LanguageChanged += (s, e) => OnPropertyChanged(nameof(StatusLabel));
    }


    // Job name
    public string Name => _job.Name;

    // Source directory path
    public string SourcePath => _job.SourcePath;

    // Destination directory path
    public string DestinationPath => _job.DestinationPath;

    // Label for the backup type (FULL or DIFF)
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


    // Current job state (Active, Inactive, Paused)
    public JobState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    // True if the job is running
    public bool IsActive => _state == JobState.Active;

    // True if the job is paused
    public bool IsPaused => _state == JobState.Paused;

    // True if the job is stopped or never started
    public bool IsInactive => _state == JobState.Inactive;


    // Translated status label for the job (Active, Paused, Inactive)
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

    // Color of the status badge (Green=Active, Orange=Paused, Gray=Inactive)
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


    // Progress percentage (0-100)
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    // Total number of files to process
    public int? TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    // Number of remaining files to process
    public int? RemainingFiles
    {
        get => _remainingFiles;
        set => SetProperty(ref _remainingFiles, value);
    }

    // Total size to transfer in bytes
    public long? TotalSize
    {
        get => _totalSize;
        set => SetProperty(ref _totalSize, value);
    }

    // Remaining size to transfer in bytes
    public long? RemainingSize
    {
        get => _remainingSize;
        set => SetProperty(ref _remainingSize, value);
    }

    // Formatted text for remaining files (e.g., "5/10 files")
    public string RemainingFilesText
    {
        get
        {
            if (RemainingFiles == null || TotalFiles == null)
                return "";
            return $"{RemainingFiles}/{TotalFiles} files";
        }
    }

    // Formatted progress text (e.g., "45.5%")
    public string ProgressText
    {
        get => $"{Progress:F1}%";
    }

    // Formatted remaining size text in MB (e.g., "12.50 MB")
    public string RemainingSizeText
    {
        get
        {
            if (RemainingSize == null)
                return "0.00 MB";
            return $"{RemainingSize:F2} MB";
        }
    }

    // True if progress should be displayed (Active or Paused)
    public bool IsProgressVisible => IsActive || IsPaused;


    // Indicates if the job is selected
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

    // Indicates if the job card is expanded to see more details
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    // True if the delete confirmation dialog is displayed
    public bool IsDeleteConfirming
    {
        get => _isDeleteConfirming;
        set => SetProperty(ref _isDeleteConfirming, value);
    }

    // Geometry data for the Play/Pause/Resume button icon
    // Play (inactive), Pause (active), Arrow (resume)
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

    // Geometry data for the Delete button icon
    // Stop (running/paused), Trash (inactive)
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

    // Background color of the Play button (Green=inactive, Orange=active/paused)
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

    // Color of the Play button
    public IBrush PlayButtonColor => IsInactive ? Brushes.Green : Brushes.Orange;



    // Play/Pause/Resume command based on state
    public ICommand PlayPauseResumeCommand { get; }

    // Command to stop the job
    public ICommand StopCommand { get; }

    // Command to delete the job
    public ICommand DeleteCommand { get; }

    // Command to show/hide job details
    public ICommand ToggleExpandCommand { get; }

    // Command to confirm deletion
    public ICommand ConfirmDeleteCommand { get; }

    // Command to cancel deletion
    public ICommand CancelDeleteCommand { get; }


    // Raised when user clicks Play
    public event EventHandler<JobViewModel>? PlayRequested;

    // Raised when user clicks Pause
    public event EventHandler<JobViewModel>? PauseRequested;

    // Raised when user clicks Resume
    public event EventHandler<JobViewModel>? ResumeRequested;

    // Raised when user clicks Stop
    public event EventHandler<JobViewModel>? StopRequested;

    // Raised when user confirms deletion
    public event EventHandler<JobViewModel>? DeleteRequested;

    // Raised when job selection changes
    public event EventHandler<(JobViewModel, bool)>? SelectionChanged;


    // Applies a state update from the Core layer
    // Updates job state and progress
    // @param entry - StateEntry from Core containing updated info
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

    // Handles click on Play/Pause/Resume button
    // Changes state based on current state and raises appropriate event
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

    // Handles click on Stop button
    private void OnStop()
    {
        StopRequested?.Invoke(this, this);
    }

    // Handles click on Delete button
    // If running: stops the job
    // If inactive: shows deletion confirmation
    private void OnDelete()
    {
        if (IsActive || IsPaused)
        {
            // If running, stop first
            StopRequested?.Invoke(this, this);
        }
        else
        {
            // If inactive, show confirmation
            IsDeleteConfirming = !IsDeleteConfirming;
        }
    }

    // Handles deletion confirmation
    // Closes the dialog and raises DeleteRequested event
    private void OnConfirmDelete()
    {
        IsDeleteConfirming = false;
        DeleteRequested?.Invoke(this, this);
    }

    // Handles deletion cancellation
    // Simply closes the dialog
    private void OnCancelDelete()
    {
        IsDeleteConfirming = false;
    }

    // Toggles job details display
    private void OnToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}
