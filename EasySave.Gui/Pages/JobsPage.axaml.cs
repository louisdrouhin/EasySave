using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EasySave.Core;
using EasySave.Core.Localization;
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
    private DispatcherTimer? _updateTimer;

    public JobsPage()
    {
        _jobManager = null;
        InitializeComponent();
    }

    public JobsPage(JobManager jobManager)
    {
        _jobManager = jobManager;
        InitializeComponent();

        LocalizationManager.LanguageChanged += OnLanguageChanged;

        var headerTitle = this.FindControl<TextBlock>("HeaderTitleText");
        if (headerTitle != null) headerTitle.Text = LocalizationManager.Get("JobsPage_Header_Title");

        var headerSubtitle = this.FindControl<TextBlock>("HeaderSubtitleText");
        if (headerSubtitle != null) headerSubtitle.Text = LocalizationManager.Get("JobsPage_Header_Subtitle");

        var createJobButton = this.FindControl<Button>("CreateJobButton");
        if (createJobButton != null)
        {
            createJobButton.Content = LocalizationManager.Get("JobsPage_Button_NewJob");
            createJobButton.Click += CreateJobButton_Click;
        }

        LoadJobs();

        // Initialiser le watcher et le timer immédiatement après LoadJobs()
        InitializeStateWatcher();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Ne réinitialiser que si pas déjà fait dans le constructeur
        if (_updateTimer == null || !_updateTimer.IsEnabled)
        {
            InitializeStateWatcher();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _watcher?.Dispose();
        _watcher = null;
        _updateTimer?.Stop();
        _updateTimer = null;
    }

    private void LoadJobs()
    {
        if (_jobManager == null) return;

        var jobs = _jobManager.GetJobs();
        _jobCards.Clear();

        System.Diagnostics.Debug.WriteLine($"[JobsPage.LoadJobs] Loading {jobs.Count} jobs");

        if (JobsStackPanel != null)
        {
            JobsStackPanel.Children.Clear();

            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                System.Diagnostics.Debug.WriteLine($"[JobsPage.LoadJobs] Creating card for job '{job.Name}' (#{i + 1})");

                var card = new JobCard(job, i + 1);
                card.PlayClicked += OnJobPlay;
                card.PauseClicked += OnJobPause;
                card.ResumeClicked += OnJobResume;
                card.StopClicked += OnJobStop;
                card.DeleteClicked += OnJobDelete;
                JobsStackPanel.Children.Add(card);
                _jobCards[job.Name] = card;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[JobsPage.LoadJobs] All cards created, dictionary has {_jobCards.Count} entries");
        System.Diagnostics.Debug.WriteLine($"[JobsPage.LoadJobs] Dictionary keys: {string.Join(", ", _jobCards.Keys)}");

        UpdateStateContent();
    }

    private void InitializeStateWatcher()
    {
        try
        {
            var configParser = new ConfigParser("config.json");
            _stateFilePath = configParser.Config?["config"]?["stateFilePath"]?.GetValue<string>() ?? "state.json";

            // Convertir en chemin absolu
            if (!Path.IsPathRooted(_stateFilePath))
            {
                _stateFilePath = Path.Combine(AppContext.BaseDirectory, _stateFilePath);
            }

            // Normaliser le chemin
            _stateFilePath = Path.GetFullPath(_stateFilePath);

            System.Diagnostics.Debug.WriteLine($"[JobsPage] State file path: {_stateFilePath}");
            System.Diagnostics.Debug.WriteLine($"[JobsPage] File exists: {File.Exists(_stateFilePath)}");

            string directory = Path.GetDirectoryName(_stateFilePath) ?? AppContext.BaseDirectory;
            string fileName = Path.GetFileName(_stateFilePath);

            System.Diagnostics.Debug.WriteLine($"[JobsPage] Watching directory: {directory}");
            System.Diagnostics.Debug.WriteLine($"[JobsPage] Watching file: {fileName}");

            // S'assurer que le répertoire existe et est valide
            if (Directory.Exists(directory) && !string.IsNullOrEmpty(fileName))
            {
                try
                {
                    _watcher = new FileSystemWatcher(directory)
                    {
                        Filter = fileName,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };
                    _watcher.Changed += OnStateFileChanged;
                    System.Diagnostics.Debug.WriteLine($"[JobsPage] FileSystemWatcher initialized successfully");
                }
                catch (Exception watcherEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobsPage] FileSystemWatcher creation failed: {watcherEx.Message}");
                    // Continue sans le watcher, le timer suffira
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[JobsPage] Directory does not exist or invalid filename: {directory}");
            }

            // Timer de polling pour mise à jour régulière (toutes les 500ms)
            // Plus fiable que FileSystemWatcher pour jobs simultanés
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateTimer.Tick += (s, e) => UpdateStateContent();
            _updateTimer.Start();
            System.Diagnostics.Debug.WriteLine($"[JobsPage] DispatcherTimer started (interval: 500ms)");

            UpdateStateContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JobsPage] Error initializing state watcher: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[JobsPage] Error initializing state watcher: {ex.Message}");
        }
    }

    private void OnStateFileChanged(object sender, FileSystemEventArgs e)
    {
        // Mise à jour immédiate en plus du polling
        Dispatcher.UIThread.Post(UpdateStateContent);
    }

    private void UpdateStateContent()
    {
        // Debug: Vérifier que la méthode est appelée
        System.Diagnostics.Debug.WriteLine($"[JobsPage] UpdateStateContent called at {DateTime.Now:HH:mm:ss.fff}");

        // Retry mechanism pour gérer les conflits de lecture/écriture
        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("[JobsPage] State file path is empty or null");
                    return;
                }

                if (!File.Exists(_stateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[JobsPage] State file not found: {_stateFilePath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[JobsPage] Reading state file: {_stateFilePath}");

                using (var fs = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string content = sr.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine($"[JobsPage] State file content length: {content.Length} chars");

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        System.Diagnostics.Debug.WriteLine("[JobsPage] State file is empty");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[JobsPage] State file content: {content.Substring(0, Math.Min(200, content.Length))}...");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    };

                    var states = JsonSerializer.Deserialize<List<StateEntry>>(content, options);

                    if (states != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobsPage] Found {states.Count} states, have {_jobCards.Count} cards");

                        // Le DispatcherTimer s'exécute déjà sur le thread UI, pas besoin de Post()
                        foreach (var state in states)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobsPage] Processing state for '{state.JobName}': {state.State}, Progress: {state.Progress:F1}%");

                            if (_jobCards.TryGetValue(state.JobName, out var card))
                            {
                                System.Diagnostics.Debug.WriteLine($"[JobsPage] Updating card for '{state.JobName}'");
                                card.UpdateState(state);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[JobsPage] No card found for job '{state.JobName}'");
                                System.Diagnostics.Debug.WriteLine($"[JobsPage] Available cards: {string.Join(", ", _jobCards.Keys)}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[JobsPage] Deserialization returned null");
                    }
                }

                // Si on arrive ici, la lecture a réussi
                return;
            }
            catch (IOException ex) when (retry < 2)
            {
                // Le fichier est verrouillé, on attend un peu avant de réessayer
                System.Diagnostics.Debug.WriteLine($"[JobsPage] IO error (retry {retry}): {ex.Message}");
                Task.Delay(50).Wait();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[JobsPage] JSON parsing error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JobsPage] JSON parsing error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JobsPage] Stack trace: {ex.StackTrace}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JobsPage] Error updating state: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JobsPage] Error updating state: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JobsPage] Stack trace: {ex.StackTrace}");
                return;
            }
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
            var errorMessage = LocalizationManager.GetFormatted("JobsPage_Error_BusinessAppMessage", runningBusinessApp);
            var errorTitle = LocalizationManager.Get("JobsPage_Error_BusinessAppTitle");

            var mainWindow = (Window?)TopLevel.GetTopLevel(this);
            if (mainWindow != null)
            {
                var errorDialog = new ErrorDialog(errorTitle, errorMessage);
                await errorDialog.ShowDialog(mainWindow);
            }
            return;
        }

        var passwordDialog = new PasswordDialog();
        var mainWindow2 = (Window?)TopLevel.GetTopLevel(this);
        if (mainWindow2 != null)
        {
            var password = await passwordDialog.ShowDialog<string?>(mainWindow2);
            if (password != null && _jobManager != null)
            {
                System.Diagnostics.Debug.WriteLine($"[JobsPage] Launching job '{job.Name}' in background");

                // Lancer le job en arrière-plan sans bloquer l'UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _jobManager.LaunchJobAsync(job, password);
                        System.Diagnostics.Debug.WriteLine($"[JobsPage] Job '{job.Name}' completed successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobsPage] Job '{job.Name}' failed: {ex.Message}");

                        // Afficher l'erreur sur le thread UI
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var errorDialog = new ErrorDialog(
                                LocalizationManager.Get("JobsPage_Error_JobFailedTitle"),
                                LocalizationManager.GetFormatted("JobsPage_Error_JobFailedMessage", job.Name, ex.Message));
                            await errorDialog.ShowDialog(mainWindow2);
                        });
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

    private void OnJobPause(object? sender, Job job)
    {
        if (_jobManager != null)
        {
            System.Diagnostics.Debug.WriteLine($"[JobsPage] Pausing job '{job.Name}'");
            _jobManager.PauseJob(job.Name);
        }
    }

    private void OnJobResume(object? sender, Job job)
    {
        if (_jobManager != null)
        {
            System.Diagnostics.Debug.WriteLine($"[JobsPage] Resuming job '{job.Name}'");
            _jobManager.ResumeJob(job.Name);
        }
    }

    private void OnJobStop(object? sender, Job job)
    {
        if (_jobManager != null)
        {
            System.Diagnostics.Debug.WriteLine($"[JobsPage] Stopping job '{job.Name}'");
            _jobManager.StopJob(job.Name);
        }
    }

    private void OnLanguageChanged(object? sender, EasySave.Core.Localization.LanguageChangedEventArgs e)
    {
        try
        {
            var headerTitle = this.FindControl<TextBlock>("HeaderTitleText");
            if (headerTitle != null) headerTitle.Text = LocalizationManager.Get("JobsPage_Header_Title");

            var headerSubtitle = this.FindControl<TextBlock>("HeaderSubtitleText");
            if (headerSubtitle != null) headerSubtitle.Text = LocalizationManager.Get("JobsPage_Header_Subtitle");

            var createJobButton = this.FindControl<Button>("CreateJobButton");
            if (createJobButton != null) createJobButton.Content = LocalizationManager.Get("JobsPage_Button_NewJob");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating language in JobsPage: {ex.Message}");
        }
    }
}