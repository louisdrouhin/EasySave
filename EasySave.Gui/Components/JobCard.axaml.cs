using Avalonia.Controls;
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
}
