using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Gui.Commands;
using EasySave.Models;

namespace EasySave.Gui.ViewModels;

/// <summary>
/// ViewModel for the Jobs page - manages job list, selection, and operations
/// </summary>
public class JobsPageViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private bool _isSelectionBarVisible;
    private string _selectionCountText = "";

    public JobsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

        // Collections
        Jobs = new ObservableCollection<JobViewModel>();

        // Commands
        CreateJobCommand = new RelayCommand(_ => OnCreateJob());
        DeselectAllCommand = new RelayCommand(_ => OnDeselectAll());
        RunSelectedCommand = new RelayCommand(_ => OnRunSelected());

        // Load initial jobs
        LoadJobs();

        // Subscribe to Core events
        _jobManager.JobStateChanged += OnJobStateChanged;
        _jobManager.JobCreated += OnJobCreated;
        _jobManager.JobRemoved += OnJobRemoved;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    #region Properties

    public ObservableCollection<JobViewModel> Jobs { get; }

    public bool IsSelectionBarVisible
    {
        get => _isSelectionBarVisible;
        set => SetProperty(ref _isSelectionBarVisible, value);
    }

    public string SelectionCountText
    {
        get => _selectionCountText;
        set => SetProperty(ref _selectionCountText, value);
    }

    public string HeaderTitle => LocalizationManager.Get("JobsPage_Header_Title") ?? "Jobs";
    public string HeaderSubtitle => LocalizationManager.Get("JobsPage_Header_Subtitle") ?? "";
    public string CreateJobLabel => LocalizationManager.Get("JobsPage_Button_NewJob") ?? "New Job";
    public string DeselectAllLabel => LocalizationManager.Get("JobsPage_DeselectAll") ?? "Deselect All";
    public string RunSelectedLabel
    {
        get
        {
            var selectedCount = Jobs.Count(j => j.IsSelected);
            return LocalizationManager.GetFormatted("JobsPage_RunSelected", selectedCount) ?? $"Run Selected ({selectedCount})";
        }
    }

    #endregion

    #region Commands

    public ICommand CreateJobCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RunSelectedCommand { get; }

    #endregion

    #region Events

    public event EventHandler? CreateJobRequested;
    public event EventHandler<JobViewModel>? PlayJobRequested;
    public event EventHandler? RunSelectedRequested;
    public event EventHandler<string>? ErrorOccurred;

    #endregion

    #region Methods

    private void LoadJobs()
    {
        var jobs = _jobManager.GetJobs();
        foreach (var job in jobs)
        {
            var vm = new JobViewModel(job);
            SubscribeToJobVM(vm);
            Jobs.Add(vm);
        }
    }

    private void SubscribeToJobVM(JobViewModel vm)
    {
        vm.PlayRequested += OnJobPlayRequested;
        vm.PauseRequested += OnJobPauseRequested;
        vm.ResumeRequested += OnJobResumeRequested;
        vm.StopRequested += OnJobStopRequested;
        vm.DeleteRequested += OnJobDeleteRequested;
        vm.SelectionChanged += (s, e) => UpdateSelectionBar();
    }

    private void OnCreateJob()
    {
        CreateJobRequested?.Invoke(this, EventArgs.Empty);
    }

    public void OnJobCreated(string name, JobType type, string sourcePath, string destinationPath)
    {
        try
        {
            _jobManager.CreateJob(name, type, sourcePath, destinationPath);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private void OnDeselectAll()
    {
        foreach (var job in Jobs)
        {
            job.IsSelected = false;
        }
        UpdateSelectionBar();
    }

    private void OnRunSelected()
    {
        RunSelectedRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task ExecutePlayJob(JobViewModel vm, string password)
    {
        try
        {
            // Launch job completely in background without waiting
            _ = Task.Run(() =>
            {
                _jobManager.LaunchJob(vm.Job, password);
            });

            // Deselect the job after launching
            vm.IsSelected = false;

            // Return immediately so dialog closes
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    public async Task ExecuteRunSelected(List<JobViewModel> selected, string password)
    {
        try
        {
            foreach (var vm in selected)
            {
                await ExecutePlayJob(vm, password);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    public string? CheckBusinessApp()
    {
        return _jobManager.CheckBusinessApplications();
    }

    public List<JobViewModel> GetSelectedJobs()
    {
        return Jobs.Where(j => j.IsSelected).ToList();
    }

    private void UpdateSelectionBar()
    {
        var selectedCount = Jobs.Count(j => j.IsSelected);
        IsSelectionBarVisible = selectedCount > 0;
        SelectionCountText = selectedCount > 0 ? $"{selectedCount} selected" : "";
        OnPropertyChanged(nameof(RunSelectedLabel));
    }

    private void OnJobPlayRequested(object? sender, JobViewModel vm)
    {
        PlayJobRequested?.Invoke(this, vm);
    }

    private void OnJobPauseRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.PauseJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private void OnJobResumeRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.ResumeJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private void OnJobStopRequested(object? sender, JobViewModel vm)
    {
        try
        {
            _jobManager.StopJob(vm.Job);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private void OnJobDeleteRequested(object? sender, JobViewModel vm)
    {
        var index = Jobs.IndexOf(vm);
        if (index >= 0)
        {
            try
            {
                _jobManager.removeJob(index);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }
    }

    private void OnJobStateChanged(object? sender, StateEntry entry)
    {
        System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Received event: {entry.JobName} = {entry.State}");

        var jobVM = Jobs.FirstOrDefault(j => j.Name == entry.JobName);
        if (jobVM != null)
        {
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Applying state for {entry.JobName}");
            // Use Invoke to ensure immediate execution (blocks until done)
            Dispatcher.UIThread.Invoke(() => jobVM.ApplyState(entry), DispatcherPriority.Default);
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] State applied for {entry.JobName}, current state: {jobVM.State}");
        }
    }

    private void OnJobCreated(object? sender, Job job)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new JobViewModel(job);
            SubscribeToJobVM(vm);
            Jobs.Add(vm);
        });
    }

    private void OnJobRemoved(object? sender, int index)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (index >= 0 && index < Jobs.Count)
            {
                Jobs.RemoveAt(index);
            }
            UpdateSelectionBar();
        });
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CreateJobLabel));
        OnPropertyChanged(nameof(DeselectAllLabel));
        OnPropertyChanged(nameof(RunSelectedLabel));
    }

    #endregion

    public void Dispose()
    {
        _jobManager.JobStateChanged -= OnJobStateChanged;
        _jobManager.JobCreated -= OnJobCreated;
        _jobManager.JobRemoved -= OnJobRemoved;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;

        foreach (var job in Jobs)
        {
            job.PlayRequested -= OnJobPlayRequested;
            job.PauseRequested -= OnJobPauseRequested;
            job.ResumeRequested -= OnJobResumeRequested;
            job.StopRequested -= OnJobStopRequested;
            job.DeleteRequested -= OnJobDeleteRequested;
        }
    }
}
