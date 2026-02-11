using Avalonia.Controls;
using System;
using EasySave.Core;

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

        foreach (var job in jobs)
        {
            Console.WriteLine($"Job: {job.Name} - {job.Type} - {job.SourcePath} -> {job.DestinationPath}");
        }

        if (JobsItemsControl != null)
        {
            JobsItemsControl.ItemsSource = jobs;
            Console.WriteLine("Jobs bound to ItemsControl");
        }
        else
        {
            Console.WriteLine("JobsItemsControl is null!");
        }
    }
}
