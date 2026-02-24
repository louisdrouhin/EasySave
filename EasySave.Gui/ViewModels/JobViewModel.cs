using System;
using System.Windows.Input;
using Avalonia.Media;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;
using EasySave.Models;

namespace EasySave.Gui.ViewModels;

/// <summary>
/// ViewModel for a single Job - handles job state, properties, and user interactions
/// </summary>
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

    public Job Job => _job;

    public JobViewModel(Job job)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));

        // Commands
        PlayPauseResumeCommand = new RelayCommand(_ => OnPlayPauseResume());
        StopCommand = new RelayCommand(_ => OnStop());
        DeleteCommand = new RelayCommand(_ => OnDelete());
        ToggleExpandCommand = new RelayCommand(_ => OnToggleExpand());
        ConfirmDeleteCommand = new RelayCommand(_ => OnConfirmDelete());
        CancelDeleteCommand = new RelayCommand(_ => OnCancelDelete());

        // Subscribe to language changes for localized status
        LocalizationManager.LanguageChanged += (s, e) => OnPropertyChanged(nameof(StatusLabel));
    }

    #region Properties - Job Info

    public string Name => _job.Name;
    public string SourcePath => _job.SourcePath;
    public string DestinationPath => _job.DestinationPath;

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

    #endregion

    #region Properties - State

    public JobState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public bool IsActive => _state == JobState.Active;
    public bool IsPaused => _state == JobState.Paused;
    public bool IsInactive => _state == JobState.Inactive;

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

    #endregion

    #region Properties - Progress

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public int? TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    public int? RemainingFiles
    {
        get => _remainingFiles;
        set => SetProperty(ref _remainingFiles, value);
    }

    public long? TotalSize
    {
        get => _totalSize;
        set => SetProperty(ref _totalSize, value);
    }

    public long? RemainingSize
    {
        get => _remainingSize;
        set => SetProperty(ref _remainingSize, value);
    }

    public string RemainingFilesText
    {
        get
        {
            if (RemainingFiles == null || TotalFiles == null)
                return "";
            return $"{RemainingFiles}/{TotalFiles} files";
        }
    }

    public string ProgressText
    {
        get => $"{Progress:F1}%";
    }

    public string RemainingSizeText
    {
        get
        {
            if (RemainingSize == null)
                return "0.00 MB";
            return $"{RemainingSize:F2} MB";
        }
    }

    public bool IsProgressVisible => IsActive || IsPaused;

    #endregion

    #region Properties - UI State

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

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsDeleteConfirming
    {
        get => _isDeleteConfirming;
        set => SetProperty(ref _isDeleteConfirming, value);
    }

    public Geometry PlayPauseIconData
    {
        get
        {
            // Return icon based on state: Play (inactive), Pause (active), Arrow/Resume (paused)
            if (IsActive)
                return Geometry.Parse("M14,19H18V5H14M6,19H10V5H6V19Z"); // Pause
            else if (IsPaused)
                return Geometry.Parse("M5,12H19M15,8L19,12L15,16"); // Arrow right (Resume)
            else
                return Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"); // Play
        }
    }

    public Geometry DeleteIconData
    {
        get
        {
            // Show Stop icon if active/paused, Trash icon if inactive
            if (IsActive || IsPaused)
                return Geometry.Parse("M18,18H6V6H18V18Z"); // Stop icon
            else
                return Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z"); // Trash icon
        }
    }

    public IBrush PlayButtonBackground
    {
        get
        {
            // Green for Inactive (Play), Orange for both Active (Pause) and Paused (Resume)
            if (IsInactive)
                return new SolidColorBrush(Color.Parse("#22C55E")); // Green
            else // IsActive or IsPaused
                return new SolidColorBrush(Color.Parse("#F97316")); // Orange for both
        }
    }

    public IBrush PlayButtonColor => IsInactive ? Brushes.Green : Brushes.Orange;

    #endregion

    #region Commands

    public ICommand PlayPauseResumeCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleExpandCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDeleteCommand { get; }

    #endregion

    #region Events

    public event EventHandler<JobViewModel>? PlayRequested;
    public event EventHandler<JobViewModel>? PauseRequested;
    public event EventHandler<JobViewModel>? ResumeRequested;
    public event EventHandler<JobViewModel>? StopRequested;
    public event EventHandler<JobViewModel>? DeleteRequested;
    public event EventHandler<(JobViewModel, bool)>? SelectionChanged;

    #endregion

    #region Methods

    /// <summary>
    /// Apply a state update from the Core layer to this ViewModel
    /// </summary>
    public void ApplyState(StateEntry entry)
    {
        if (entry.JobName != _job.Name)
            return;

        // Log state changes for debugging
        var oldState = _state;
        System.Diagnostics.Debug.WriteLine($"[{_job.Name}] ApplyState called: {oldState} → {entry.State}");

        // Apply the state change immediately - trust the backend state
        State = entry.State;
        System.Diagnostics.Debug.WriteLine($"[{_job.Name}] State applied: {_state}");

        // Update progress if available
        Progress = entry.Progress ?? 0;
        TotalFiles = entry.TotalFiles;
        RemainingFiles = entry.RemainingFiles;
        TotalSize = entry.TotalSizeToTransfer;
        RemainingSize = entry.RemainingSizeToTransfer;

        // Refresh computed properties
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

    private void OnStop()
    {
        StopRequested?.Invoke(this, this);
    }

    private void OnDelete()
    {
        if (IsActive || IsPaused)
        {
            // If job is running, stop it
            StopRequested?.Invoke(this, this);
        }
        else
        {
            // If job is inactive, toggle confirmation mode
            IsDeleteConfirming = !IsDeleteConfirming;
        }
    }

    private void OnConfirmDelete()
    {
        IsDeleteConfirming = false;
        DeleteRequested?.Invoke(this, this);
    }

    private void OnCancelDelete()
    {
        IsDeleteConfirming = false;
    }

    private void OnToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    #endregion
}
