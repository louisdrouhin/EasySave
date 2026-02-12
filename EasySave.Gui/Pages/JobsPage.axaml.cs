using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Core;
using EasySave.GUI.Components;
using EasySave.GUI.Dialogs;
using EasySave.Models;

namespace EasySave.GUI.Pages;

public partial class JobsPage : UserControl
{
    private readonly JobManager? _jobManager;
    private FileSystemWatcher? _watcher;
    private string _stateFilePath = string.Empty;
    private Dictionary<string, JobCard> _jobCards = new();

    public JobsPage()
    {
        _jobManager = null;
        InitializeComponent();
    }

    public JobsPage(JobManager jobManager)
    {
        _jobManager = jobManager;
        InitializeComponent();
        LoadJobs();

        var createJobButton = this.FindControl<Button>("CreateJobButton");
        if (createJobButton != null)
        {
            createJobButton.Click += CreateJobButton_Click;
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeStateWatcher();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _watcher?.Dispose();
        _watcher = null;
    }

    private void LoadJobs()
    {
        if (_jobManager == null) return;

        var jobs = _jobManager.GetJobs();
        _jobCards.Clear();

        if (JobsStackPanel != null)
        {
            JobsStackPanel.Children.Clear();

            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                var card = new JobCard(job, i + 1);
                card.PlayClicked += OnJobPlay;
                card.DeleteClicked += OnJobDelete;
                JobsStackPanel.Children.Add(card);
                _jobCards[job.Name] = card;
            }
        }
        
        UpdateStateContent();
    }

    private void InitializeStateWatcher()
    {
        try
        {
            var configParser = new ConfigParser("config.json");
            _stateFilePath = configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>() ?? "state.json";

            if (!Path.IsPathRooted(_stateFilePath))
            {
                string executionDirState = Path.Combine(AppContext.BaseDirectory, _stateFilePath);
                string projectRootState = Path.Combine(AppContext.BaseDirectory, "../../../../../", _stateFilePath);
                
                if (File.Exists(executionDirState)) _stateFilePath = executionDirState;
                else if (File.Exists(projectRootState)) _stateFilePath = Path.GetFullPath(projectRootState);
            }

            string directory = Path.GetDirectoryName(_stateFilePath) ?? AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(directory)) directory = ".";
            string fileName = Path.GetFileName(_stateFilePath);

            if (Directory.Exists(directory))
            {
                _watcher = new FileSystemWatcher(directory)
                {
                    Filter = fileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _watcher.Changed += OnStateFileChanged;
            }
            
            UpdateStateContent();
        }
        catch (Exception) { }
    }

    private void OnStateFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateStateContent);
    }

    private void UpdateStateContent()
    {
        try
        {
            if (!File.Exists(_stateFilePath)) 
            {
                // Console.WriteLine($"[JobsPage] State file not found: {_stateFilePath}");
                return;
            }

            using (var fs = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string content = sr.ReadToEnd();
                if (string.IsNullOrWhiteSpace(content)) return;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                var states = JsonSerializer.Deserialize<List<StateEntry>>(content, options);
                
                if (states != null)
                {
                    // Console.WriteLine($"[JobsPage] Deserialized {states.Count} states.");
                    foreach (var state in states)
                    {
                        if (_jobCards.TryGetValue(state.JobName, out var card))
                        {
                            // Console.WriteLine($"[JobsPage] Updating card for {state.JobName}");
                            card.UpdateState(state);
                        }
                        else
                        {
                            // Console.WriteLine($"[JobsPage] Card not found for {state.JobName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[JobsPage] Error updating state: {ex.Message}");
        }
    }

    private async void CreateJobButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new CreateJobDialog();
        var mainWindow = (Window?)TopLevel.GetTopLevel(this);
        if (mainWindow != null)
        {
            var result = await dialog.ShowDialog<CreateJobDialog.JobResult?>(mainWindow);
            if (result != null)
            {
                _jobManager?.CreateJob(result.Name, result.Type, result.SourcePath, result.DestinationPath);
                LoadJobs();
            }
        }
    }

    private async void OnJobPlay(object? sender, Job job)
    {
        var runningBusinessApp = _jobManager?.CheckBusinessApplications();
        if (runningBusinessApp != null)
        {
            var errorMessage = $"Business application '{runningBusinessApp}' is running. Backup job execution blocked.";
            var mainWindow = (Window?)TopLevel.GetTopLevel(this);
            if (mainWindow != null)
            {
                var errorDialog = new ErrorDialog("Business Application Running", errorMessage);
                await errorDialog.ShowDialog(mainWindow);
            }
            return;
        }

        var passwordDialog = new PasswordDialog();
        var mainWindow2 = (Window?)TopLevel.GetTopLevel(this);
        if (mainWindow2 != null)
        {
            var password = await passwordDialog.ShowDialog<string?>(mainWindow2);
            if (password != null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _jobManager?.LaunchJob(job, password);
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }
    }

    private void OnJobDelete(object? sender, (int index, Job job) data)
    {
        var (index, job) = data;
        _jobManager?.removeJob(index);
        LoadJobs();
    }
}