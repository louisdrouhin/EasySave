using Avalonia.Controls;
using Avalonia.Media;
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
    }

    private void OnToggleExpanded()
    {
        _isExpanded = !_isExpanded;
        var toggleButton = this.FindControl<Button>("ToggleButton");
        if (toggleButton != null)
        {
            toggleButton.Content = _isExpanded ? "▼" : "▶";
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
        DeleteClicked?.Invoke(this, (_index - 1, _job));
    }

    public void UpdateState(StateEntry state)
    {
        var statusBadge = this.FindControl<Border>("StatusBadge");
        var statusTextControl = this.FindControl<TextBlock>("StatusText");
        
        if (statusBadge != null && statusTextControl != null)
        {
            statusTextControl.Text = state.State.ToString();
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
            if (stateText != null) stateText.Text = !string.IsNullOrEmpty(state.CurrentSourcePath) ? "Processing..." : "Active";

            var filesText = this.FindControl<TextBlock>("FilesText");
            if (filesText != null) 
            {
                long total = state.TotalFiles ?? 0;
                long remaining = state.RemainingFiles ?? 0;
                long processed = total - remaining;
                filesText.Text = $"{processed:N0} / {total:N0} Files";
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
