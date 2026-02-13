using Avalonia.Controls;
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

    public event EventHandler<Job>? PlayClicked;
    public event EventHandler<(int, Job)>? DeleteClicked;

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

    private void OnPlayClicked()
    {
        PlayClicked?.Invoke(this, _job);
    }

    private void OnDeleteClicked()
    {
        ToggleDeleteMode(true);
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
        var statusBadge = this.FindControl<Border>("StatusBadge");
        var statusTextControl = this.FindControl<TextBlock>("StatusText");
        
        if (statusBadge != null && statusTextControl != null)
        {
            statusTextControl.Text = state.State == JobState.Active 
                ? LocalizationManager.Get("JobCard_Status_Active") 
                : LocalizationManager.Get("JobCard_Status_Inactive");

            if (state.State == JobState.Active)
            {
                statusBadge.Background = new SolidColorBrush(Color.Parse("#22C55E"));
                statusTextControl.Foreground = Brushes.White;
            }
            else
            {
                statusBadge.Background = new SolidColorBrush(Color.Parse("#E5E7EB"));
                statusTextControl.Foreground = new SolidColorBrush(Color.Parse("#374151"));
            }
        }

        var progressPanel = this.FindControl<Border>("ProgressPanel");
        if (progressPanel == null) return;

        if (state.State == JobState.Active)
        {
            progressPanel.IsVisible = true;
            
            var progressBar = this.FindControl<ProgressBar>("JobProgressBar");
            if (progressBar != null) progressBar.Value = state.Progress ?? 0;

            var percentageText = this.FindControl<TextBlock>("PercentageText");
            if (percentageText != null) percentageText.Text = $"{(state.Progress ?? 0):F1}%";

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
            }

            var sizeText = this.FindControl<TextBlock>("SizeText");
            if (sizeText != null)
            {
                long total = state.TotalSizeToTransfer ?? 0;
                long remaining = state.RemainingSizeToTransfer ?? 0;
                long processed = total - remaining;
                sizeText.Text = $"{FormatSize(processed)} / {FormatSize(total)}";
            }
        }
        else
        {
            progressPanel.IsVisible = false;
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
}
