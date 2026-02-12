using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using EasySave.Core;
using EasySave.GUI.Components;
using EasySave.GUI.Dialogs;
using EasySave.Models;

namespace EasySave.GUI.Pages;

public partial class JobsPage : UserControl
{
    private readonly JobManager? _jobManager;

    public JobsPage()
    {
        _jobManager = null;
        InitializeComponent();
    }

    public JobsPage(JobManager jobManager)
    {
        Console.WriteLine("JobsPage constructor called with JobManager");
        _jobManager = jobManager;
        InitializeComponent();
        Console.WriteLine("InitializeComponent completed");
        LoadJobs();

        var createJobButton = this.FindControl<Button>("CreateJobButton");
        if (createJobButton != null)
        {
            createJobButton.Click += CreateJobButton_Click;
        }
    }

    private void LoadJobs()
    {
        if (_jobManager == null)
        {
            Console.WriteLine("JobManager is null!");
            return;
        }

        Console.WriteLine("Loading jobs from JobManager...");
        var jobs = _jobManager.GetJobs();
        Console.WriteLine($"Found {jobs.Count} jobs");

        if (JobsStackPanel != null)
        {
            JobsStackPanel.Children.Clear();

            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Console.WriteLine($"Job {i + 1}: {job.Name} - {job.Type} - {job.SourcePath} -> {job.DestinationPath}");

                var card = new JobCard(job, i + 1);
                card.PlayClicked += OnJobPlay;
                card.DeleteClicked += OnJobDelete;
                JobsStackPanel.Children.Add(card);
            }

            Console.WriteLine("Jobs added to StackPanel");
        }
        else
        {
            Console.WriteLine("JobsStackPanel is null!");
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
                Console.WriteLine($"Creating job: {result.Name}");
                _jobManager?.CreateJob(result.Name, result.Type, result.SourcePath, result.DestinationPath);
                LoadJobs();
            }
        }
    }

    private async void OnJobPlay(object? sender, Job job)
    {
        Console.WriteLine($"Playing job: {job.Name}");

        var runningBusinessApp = _jobManager?.CheckBusinessApplications();
        if (runningBusinessApp != null)
        {
            var errorMessage = $"Business application '{runningBusinessApp}' is running. Backup job execution blocked.";
            Console.WriteLine(errorMessage);

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
                try
                {
                    _jobManager?.LaunchJob(job, password);
                    Console.WriteLine($"Job {job.Name} completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error launching job: {ex.Message}");
                }
            }
        }
    }

    private void OnJobDelete(object? sender, (int index, Job job) data)
    {
        var (index, job) = data;
        Console.WriteLine($"Deleting job at index {index}: {job.Name}");
        _jobManager?.removeJob(index);
        LoadJobs();
    }
}
