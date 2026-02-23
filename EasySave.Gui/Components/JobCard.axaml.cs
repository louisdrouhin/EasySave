using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EasySave.Core.Localization;
using EasySave.Models;
using System;

namespace EasySave.GUI.Components;

public partial class JobCard : UserControl
{
    private Job _job = null!;
    private int _index = 0;
    private bool _isExpanded = false;
    private JobState _currentState = JobState.Inactive;

    public event EventHandler<Job>? PlayClicked;
    public event EventHandler<Job>? PauseClicked;
    public event EventHandler<Job>? ResumeClicked;
    public event EventHandler<Job>? StopClicked;
    public event EventHandler<(int, Job)>? DeleteClicked;
    public event EventHandler<(Job job, bool isSelected)>? SelectionChanged;

    public JobCard()
    {
        InitializeComponent();

        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null) statusText.Text = LocalizationManager.Get("JobCard_Status_Inactive");

        var inProgressText = this.FindControl<TextBlock>("InProgressText");
        if (inProgressText != null) inProgressText.Text = LocalizationManager.Get("JobCard_Progress_InProgress");

        var filesLabel = this.FindControl<TextBlock>("FilesLabel");
        if (filesLabel != null) filesLabel.Text = LocalizationManager.Get("JobCard_Label_Files");

        var sizeLabel = this.FindControl<TextBlock>("SizeLabel");
        if (sizeLabel != null) sizeLabel.Text = LocalizationManager.Get("JobCard_Label_Size");

        var sourceLabel = this.FindControl<TextBlock>("SourceLabel");
        if (sourceLabel != null) sourceLabel.Text = LocalizationManager.Get("JobCard_Label_Source");

        var destinationLabel = this.FindControl<TextBlock>("DestinationLabel");
        if (destinationLabel != null) destinationLabel.Text = LocalizationManager.Get("JobCard_Label_Destination");
    }

    public JobCard(Job job, int index) : this()
    {
        _job = job;
        _index = index;
        DataContext = job;

        var indexText = this.FindControl<TextBlock>("IndexText");
        if (indexText != null)
        {
            indexText.Text = $"#{index}";
        }

        var jobTypeBadgeText = this.FindControl<TextBlock>("JobTypeBadgeText");
        if (jobTypeBadgeText != null)
        {
            jobTypeBadgeText.Text = job.Type == JobType.Full
                ? LocalizationManager.Get("CreateJobDialog_JobType_Full")
                : LocalizationManager.Get("CreateJobDialog_JobType_Differential");
        }

        var toggleButton = this.FindControl<Button>("ToggleButton");
        if (toggleButton != null)
        {
            toggleButton.Click += (s, e) => OnToggleExpanded();
            toggleButton.PointerEntered += (s, e) =>
            {
                toggleButton.Background = Brushes.Transparent;
            };
            toggleButton.PointerExited += (s, e) =>
            {
                toggleButton.Background = Brushes.Transparent;
            };
        }

        var playButton = this.FindControl<Button>("PlayButton");
        if (playButton != null)
        {
            playButton.Click += (s, e) => OnPlayClicked();
        }

        var deleteButton = this.FindControl<Button>("DeleteButton");
        if (deleteButton != null)
        {
            deleteButton.Click += (s, e) => OnDeleteClicked();
        }

        var cancelDeleteButton = this.FindControl<Button>("CancelDeleteButton");
        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.Click += (s, e) => OnCancelDeleteClicked();
        }

        var confirmDeleteButton = this.FindControl<Button>("ConfirmDeleteButton");
        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.Click += (s, e) => OnConfirmDeleteClicked();
        }

        var selectCheckBox = this.FindControl<CustomCheckBox>("SelectCheckBox");
        if (selectCheckBox != null)
        {
            selectCheckBox.CheckedChanged += (s, e) => OnSelectCheckBoxChanged();
        }
    }

    private void OnToggleExpanded()
    {
        _isExpanded = !_isExpanded;
        var toggleIcon = this.FindControl<PathIcon>("ToggleIcon");
        if (toggleIcon != null)
        {
            var resourceKey = _isExpanded ? "ChevronDownIcon" : "ChevronRightIcon";
            if (this.TryGetResource(resourceKey, out var geometry))
            {
                toggleIcon.Data = (Geometry)geometry!;
            }
        }

        var detailsPanel = this.FindControl<Border>("DetailsPanel");
        if (detailsPanel != null)
        {
            detailsPanel.IsVisible = _isExpanded;
        }
    }

    private void OnSelectCheckBoxChanged()
    {
        var selectCheckBox = this.FindControl<CustomCheckBox>("SelectCheckBox");
        if (selectCheckBox != null)
        {
            SelectionChanged?.Invoke(this, (_job, selectCheckBox.IsChecked));
        }
    }

    private void OnPlayClicked()
    {
        if (_currentState == JobState.Inactive)
        {
            PlayClicked?.Invoke(this, _job);
        }
        else if (_currentState == JobState.Active)
        {
            PauseClicked?.Invoke(this, _job);
        }
        else if (_currentState == JobState.Paused)
        {
            ResumeClicked?.Invoke(this, _job);
        }
    }

    private void OnDeleteClicked()
    {
        if (_currentState == JobState.Active || _currentState == JobState.Paused)
        {
            // Stop the job
            StopClicked?.Invoke(this, _job);
        }
        else
        {
            // Delete mode
            ToggleDeleteMode(true);
        }
    }

    private void OnCancelDeleteClicked()
    {
        ToggleDeleteMode(false);
    }

    private void OnConfirmDeleteClicked()
    {
        ToggleDeleteMode(false);
        DeleteClicked?.Invoke(this, (_index - 1, _job));
    }

    private void ToggleDeleteMode(bool isDeleting)
    {
        var playButton = this.FindControl<Button>("PlayButton");
        var deleteButton = this.FindControl<Button>("DeleteButton");
        var cancelButton = this.FindControl<Button>("CancelDeleteButton");
        var confirmButton = this.FindControl<Button>("ConfirmDeleteButton");

        if (playButton != null) playButton.IsVisible = !isDeleting;
        if (deleteButton != null) deleteButton.IsVisible = !isDeleting;
        if (cancelButton != null) cancelButton.IsVisible = isDeleting;
        if (confirmButton != null) confirmButton.IsVisible = isDeleting;
    }

    public void UpdateState(StateEntry state)
    {
        System.Diagnostics.Debug.WriteLine($"[JobCard] UpdateState called for '{state.JobName}': State={state.State}, Progress={state.Progress}");

        _currentState = state.State;

        var statusBadge = this.FindControl<Border>("StatusBadge");
        var statusTextControl = this.FindControl<TextBlock>("StatusText");

        if (statusBadge != null && statusTextControl != null)
        {
            if (state.State == JobState.Active)
            {
                statusTextControl.Text = LocalizationManager.Get("JobCard_Status_Active");
                statusBadge.Background = new SolidColorBrush(Color.Parse("#22C55E"));
                statusTextControl.Foreground = Brushes.White;
                System.Diagnostics.Debug.WriteLine($"[JobCard] Status badge updated to ACTIVE (green)");
            }
            else if (state.State == JobState.Paused)
            {
                statusTextControl.Text = "PAUSED";
                statusBadge.Background = new SolidColorBrush(Color.Parse("#F59E0B"));
                statusTextControl.Foreground = Brushes.White;
                System.Diagnostics.Debug.WriteLine($"[JobCard] Status badge updated to PAUSED (orange)");
            }
            else
            {
                statusTextControl.Text = LocalizationManager.Get("JobCard_Status_Inactive");
                statusBadge.Background = new SolidColorBrush(Color.Parse("#E5E7EB"));
                statusTextControl.Foreground = new SolidColorBrush(Color.Parse("#374151"));
                System.Diagnostics.Debug.WriteLine($"[JobCard] Status badge updated to INACTIVE (gray)");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[JobCard] WARNING: StatusBadge or StatusText control not found!");
        }

        // Update Play/Pause button icon
        var playPauseIcon = this.FindControl<PathIcon>("PlayPauseIcon");
        var playButton = this.FindControl<Button>("PlayButton");
        if (playPauseIcon != null && playButton != null)
        {
            if (state.State == JobState.Active)
            {
                // Show Pause icon
                if (this.TryGetResource("PauseIcon", out var pauseGeometry))
                {
                    playPauseIcon.Data = (Geometry)pauseGeometry!;
                    playPauseIcon.Margin = new Avalonia.Thickness(0);
                }
                playButton.Classes.Remove("button-green");
                playButton.Classes.Add("button-orange");
            }
            else if (state.State == JobState.Paused)
            {
                // Show Play icon (to resume)
                if (this.TryGetResource("PlayIcon", out var playGeometry))
                {
                    playPauseIcon.Data = (Geometry)playGeometry!;
                    playPauseIcon.Margin = new Avalonia.Thickness(2, 0, 0, 0);
                }
                playButton.Classes.Remove("button-orange");
                playButton.Classes.Add("button-green");
            }
            else
            {
                // Show Play icon
                if (this.TryGetResource("PlayIcon", out var playGeometry))
                {
                    playPauseIcon.Data = (Geometry)playGeometry!;
                    playPauseIcon.Margin = new Avalonia.Thickness(2, 0, 0, 0);
                }
                playButton.Classes.Remove("button-orange");
                playButton.Classes.Add("button-green");
            }
        }

        // Update Delete/Stop button icon
        var deleteStopIcon = this.FindControl<PathIcon>("DeleteStopIcon");
        if (deleteStopIcon != null)
        {
            if (state.State == JobState.Active || state.State == JobState.Paused)
            {
                // Show Stop icon
                if (this.TryGetResource("StopIcon", out var stopGeometry))
                {
                    deleteStopIcon.Data = (Geometry)stopGeometry!;
                }
            }
            else
            {
                // Show Trash icon
                if (this.TryGetResource("TrashIcon", out var trashGeometry))
                {
                    deleteStopIcon.Data = (Geometry)trashGeometry!;
                }
            }
        }

        var progressPanel = this.FindControl<Border>("ProgressPanel");
        if (progressPanel == null)
        {
            System.Diagnostics.Debug.WriteLine("[JobCard] WARNING: ProgressPanel control not found!");
            return;
        }

        if (state.State == JobState.Active || state.State == JobState.Paused)
        {
            progressPanel.IsVisible = true;
            System.Diagnostics.Debug.WriteLine($"[JobCard] ProgressPanel made visible");

            var progressBar = this.FindControl<ProgressBar>("JobProgressBar");
            if (progressBar != null)
            {
                progressBar.Value = state.Progress ?? 0;
                System.Diagnostics.Debug.WriteLine($"[JobCard] ProgressBar value set to {state.Progress ?? 0}");
            }

            var percentageText = this.FindControl<TextBlock>("PercentageText");
            if (percentageText != null)
            {
                percentageText.Text = $"{(state.Progress ?? 0):F1}%";
                System.Diagnostics.Debug.WriteLine($"[JobCard] Percentage text set to {(state.Progress ?? 0):F1}%");
            }

            var stateText = this.FindControl<TextBlock>("StateText");
            if (stateText != null) stateText.Text = !string.IsNullOrEmpty(state.CurrentSourcePath)
                ? LocalizationManager.Get("JobCard_Progress_Processing")
                : LocalizationManager.Get("JobCard_Status_Active");

            var filesText = this.FindControl<TextBlock>("FilesText");
            if (filesText != null)
            {
                long total = state.TotalFiles ?? 0;
                long remaining = state.RemainingFiles ?? 0;
                long processed = total - remaining;
                filesText.Text = $"{processed:N0} / {total:N0}";
                System.Diagnostics.Debug.WriteLine($"[JobCard] Files text set to {processed} / {total}");
            }

            var sizeText = this.FindControl<TextBlock>("SizeText");
            if (sizeText != null)
            {
                long total = state.TotalSizeToTransfer ?? 0;
                long remaining = state.RemainingSizeToTransfer ?? 0;
                long processed = total - remaining;
                sizeText.Text = $"{FormatSize(processed)} / {FormatSize(total)}";
                System.Diagnostics.Debug.WriteLine($"[JobCard] Size text set to {FormatSize(processed)} / {FormatSize(total)}");
            }
        }
        else
        {
            progressPanel.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"[JobCard] ProgressPanel made hidden");
        }
    }

    private string FormatSize(long size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double doubleSize = size;

        while (doubleSize >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            doubleSize /= 1024;
            suffixIndex++;
        }

        return $"{doubleSize:0.##} {suffixes[suffixIndex]}";
    }

    public void SetChecked(bool value)
    {
        var selectCheckBox = this.FindControl<CustomCheckBox>("SelectCheckBox");
        if (selectCheckBox != null)
        {
            selectCheckBox.SetChecked(value);
        }
    }
}
