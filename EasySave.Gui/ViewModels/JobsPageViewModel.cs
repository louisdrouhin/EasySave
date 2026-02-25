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

// ViewModel for the Jobs page
// Manages the job list, multiple selection, and job operations
public class JobsPageViewModel : ViewModelBase
{
    private readonly JobManager _jobManager;
    private bool _isSelectionBarVisible;
    private string _selectionCountText = "";

    // Initializes the Jobs page
    // Loads existing jobs and subscribes to Core events
    // @param jobManager - central job manager
    public JobsPageViewModel(JobManager jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

        // Collections
        Jobs = new ObservableCollection<JobViewModel>();

        // Commands
        CreateJobCommand = new RelayCommand(_ => OnCreateJob());
        DeselectAllCommand = new RelayCommand(_ => OnDeselectAll());
        RunSelectedCommand = new RelayCommand(_ => OnRunSelected());

        // Loads jobs from Core
        LoadJobs();

        // Subscribes to Core events
        _jobManager.JobStateChanged += OnJobStateChanged;
        _jobManager.JobCreated += OnJobCreated;
        _jobManager.JobRemoved += OnJobRemoved;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    // Observable collection of jobs for control binding
    public ObservableCollection<JobViewModel> Jobs { get; }

    // True if the selection bar (deselect all, run selected) is visible
    public bool IsSelectionBarVisible
    {
        get => _isSelectionBarVisible;
        set => SetProperty(ref _isSelectionBarVisible, value);
    }

    // Text indicating the number of selected jobs (e.g., "2 jobs selected")
    public string SelectionCountText
    {
        get => _selectionCountText;
        set => SetProperty(ref _selectionCountText, value);
    }

    // Translated titles and labels
    public string HeaderTitle => LocalizationManager.Get("JobsPage_Header_Title") ?? "Jobs";
    public string HeaderSubtitle => LocalizationManager.Get("JobsPage_Header_Subtitle") ?? "";
    public string CreateJobLabel => LocalizationManager.Get("JobsPage_Button_NewJob") ?? "New Job";
    public string DeselectAllLabel => LocalizationManager.Get("JobsPage_DeselectAll") ?? "Deselect All";
    // Label with number of selected jobs
    public string RunSelectedLabel
    {
        get
        {
            var selectedCount = Jobs.Count(j => j.IsSelected);
            return LocalizationManager.GetFormatted("JobsPage_RunSelected", selectedCount) ?? $"Run Selected ({selectedCount})";
        }
    }


    // Command to create a new job
    public ICommand CreateJobCommand { get; }

    // Command to deselect all jobs
    public ICommand DeselectAllCommand { get; }

    // Command to run all selected jobs
    public ICommand RunSelectedCommand { get; }


    public event EventHandler? CreateJobRequested;
    public event EventHandler<JobViewModel>? PlayJobRequested;
    public event EventHandler? RunSelectedRequested;
    public event EventHandler<string>? ErrorOccurred;


    // Loads jobs from JobManager and adds them to the observable collection
    // Also subscribes to each JobViewModel's events for actions (play, pause, delete, etc.)
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

    // Subscribes to a JobViewModel's events to handle user-requested actions (play, pause, delete, etc.)
    // @param vm - the JobViewModel to subscribe to
    private void SubscribeToJobVM(JobViewModel vm)
    {
        vm.PlayRequested += OnJobPlayRequested;
        vm.PauseRequested += OnJobPauseRequested;
        vm.ResumeRequested += OnJobResumeRequested;
        vm.StopRequested += OnJobStopRequested;
        vm.DeleteRequested += OnJobDeleteRequested;
        vm.SelectionChanged += (s, e) => UpdateSelectionBar();
    }

    // Raises CreateJobRequested event to signal that user wants to create a new job
    private void OnCreateJob()
    {
        CreateJobRequested?.Invoke(this, EventArgs.Empty);
    }

    // Called when the job creation form is submitted. Attempts to create a new job via JobManager and handles potential errors.
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

    // Deselects all jobs and updates the selection bar
    private void OnDeselectAll()
    {
        foreach (var job in Jobs)
        {
            job.IsSelected = false;
        }
        UpdateSelectionBar();
    }

    // Raises RunSelectedRequested event to signal that user wants to run selected jobs
    private void OnRunSelected()
    {
        RunSelectedRequested?.Invoke(this, EventArgs.Empty);
    }

    // Runs a given job in the background and handles potential errors. Deselects the job after launch for better UX.
    // @param vm - JobViewModel of the job to execute
    // @param password - password to use for the job (if needed)
    public async Task ExecutePlayJob(JobViewModel vm, string password)
    {
        try
        {
            _ = Task.Run(() =>
            {
                _jobManager.LaunchJob(vm.Job, password);
            });

            vm.IsSelected = false;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    // Runs all selected jobs in the background and handles potential errors. Deselects all jobs after launch
    // @param selected - list of JobViewModels to execute
    // @param password - password to use for the jobs (if needed)
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

    // Checks if business applications are running
    public string? CheckBusinessApp()
    {
        return _jobManager.CheckBusinessApplications();
    }

    // Gets the list of selected jobs
    // @returns list of currently selected JobViewModels
    public List<JobViewModel> GetSelectedJobs()
    {
        return Jobs.Where(j => j.IsSelected).ToList();
    }

    // Updates the selection bar visibility and text indicating the number of selected jobs
    // Called when a job's selection changes to reflect the current selection state
    private void UpdateSelectionBar()
    {
        var selectedCount = Jobs.Count(j => j.IsSelected);
        IsSelectionBarVisible = selectedCount > 0;
        SelectionCountText = selectedCount > 0 ? $"{selectedCount} selected" : "";
        OnPropertyChanged(nameof(RunSelectedLabel));
    }

    // Handles job play request by raising PlayJobRequested event with the concerned JobViewModel
    // @param sender - object that triggered the event (usually a button in JobViewModel)
    // @param vm - JobViewModel of the job to run
    private void OnJobPlayRequested(object? sender, JobViewModel vm)
    {
        PlayJobRequested?.Invoke(this, vm);
    }

    // Handles job pause request by calling JobManager's PauseJob method and handling potential errors
    // @param sender - object that triggered the event (usually a button in JobViewModel)
    // @param vm - JobViewModel of the job to pause
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

    // Handles job resume request by calling JobManager's ResumeJob method and handling potential errors
    // @param sender - object that triggered the event (usually a button in JobViewModel)
    // @param vm - JobViewModel of the job to resume
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

    // Handles job stop request by calling JobManager's StopJob method and handling potential errors
    // @param sender - object that triggered the event (usually a button in JobViewModel)
    // @param vm - JobViewModel of the job to stop
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

    // Handles job deletion request by calling JobManager's removeJob method and handling potential errors
    // @param sender - object that triggered the event (usually a button in JobViewModel)
    // @param vm - JobViewModel of the job to delete
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

    // Handles job state changes by applying new information to the corresponding JobViewModel
    // Uses Dispatcher.Invoke to ensure UI updates are performed on the UI thread
    // @param sender - object that triggered the event (usually JobManager)
    // @param entry - StateEntry object containing job state change information

    private void OnJobStateChanged(object? sender, StateEntry entry)
    {
        System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Received event: {entry.JobName} = {entry.State}");

        var jobVM = Jobs.FirstOrDefault(j => j.Name == entry.JobName);
        if (jobVM != null)
        {
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] Applying state for {entry.JobName}");
            Dispatcher.UIThread.Invoke(() => jobVM.ApplyState(entry), DispatcherPriority.Default);
            System.Diagnostics.Debug.WriteLine($"[OnJobStateChanged] State applied for {entry.JobName}, current state: {jobVM.State}");
        }
    }

    // Handles new job creation by adding a new JobViewModel to the observable collection and subscribing to its events
    // Uses Dispatcher.Invoke to ensure UI updates are performed on the UI thread
    // @param sender - object that triggered the event (usually JobManager)
    // @param job - newly created Job to add to the list
    private void OnJobCreated(object? sender, Job job)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new JobViewModel(job);
            SubscribeToJobVM(vm);
            Jobs.Add(vm);
        });
    }

    // Handles job deletion by removing the corresponding JobViewModel from the observable collection
    // @param sender - object that triggered the event (usually JobManager)
    // @param index - index of the deleted job in the collection
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

    // Handles language changes by raising property change notifications for all elements
    // @param sender - object that triggered the event (usually LocalizationManager)
    // @param e - event arguments containing selected language information
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CreateJobLabel));
        OnPropertyChanged(nameof(DeselectAllLabel));
        OnPropertyChanged(nameof(RunSelectedLabel));
    }


    // Unsubscribes from all events to prevent memory leaks when the page is destroyed
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
