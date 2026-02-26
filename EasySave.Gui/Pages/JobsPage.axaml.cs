using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using EasySave.Gui.ViewModels;
using EasySave.GUI.Dialogs;
using EasySave.Models;

namespace EasySave.GUI.Pages;

public partial class JobsPage : UserControl
{
    private const int BusinessAppCheckInterval = 2000; // 2 seconds

    public JobsPage()
    {
        InitializeComponent();
    }

    // Checks for business applications and blocks execution with user warning
    // Displays a dialog if a business application is detected
    private async Task WaitForBusinessAppsToStop(JobsPageViewModel vm, string jobName)
    {
        string? detectedApp = vm.CheckBusinessApp();
        if (!string.IsNullOrEmpty(detectedApp))
        {
            // Business app is running, show warning dialog
            Debug.WriteLine($"[JobsPage] Business app '{detectedApp}' is running. Blocking job: {jobName}");

            var window = TopLevel.GetTopLevel(this) as Window;
            var dialog = new ErrorDialog(
                "Business Application Running",
                $"Cannot launch job '{jobName}'.\n\nThe business application '{detectedApp}' is currently running.\n\nPlease close it and try again."
            );
            await dialog.ShowDialog(window);

            // Prevent job execution by throwing to break the chain
            throw new OperationCanceledException("Job blocked due to running business application");
        }

        Debug.WriteLine($"[JobsPage] No business app detected. Safe to launch job: {jobName}");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is JobsPageViewModel vm)
        {
            // Handle CreateJob dialog request
            vm.CreateJobRequested += async (s, e) =>
            {
                try
                {
                    var window = TopLevel.GetTopLevel(this) as Window;
                    var dialog = new CreateJobDialog();
                    var result = await dialog.ShowDialog<CreateJobDialog.JobResult?>(window);

                    if (result != null)
                    {
                        vm.OnJobCreated(result.Name, result.Type, result.SourcePath, result.DestinationPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JobsPage] Error in CreateJob dialog: {ex.Message}");
                }
            };

            // Handle PlayJob dialog request (password prompt)
            vm.PlayJobRequested += async (s, jobVm) =>
            {
                try
                {
                    // Show password dialog
                    var window = TopLevel.GetTopLevel(this) as Window;
                    var passwordDialog = new PasswordDialog();
                    var password = await passwordDialog.ShowDialog<string?>(window);

                    if (password != null)
                    {
                        // Check and warn if business applications are running
                        await WaitForBusinessAppsToStop(vm, jobVm.Name);
                        await vm.ExecutePlayJob(jobVm, password);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Job blocked due to business application - already warned user
                    Debug.WriteLine($"[JobsPage] Job blocked: {nameof(OperationCanceledException)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JobsPage] Error in PlayJob: {ex.Message}");
                }
            };

            // Handle RunSelected dialog request
            vm.RunSelectedRequested += async (s, e) =>
            {
                try
                {
                    var selected = vm.GetSelectedJobs();
                    if (!selected.Any()) return;

                    // Show password dialog
                    var window = TopLevel.GetTopLevel(this) as Window;
                    var passwordDialog = new PasswordDialog();
                    var password = await passwordDialog.ShowDialog<string?>(window);

                    if (password != null)
                    {
                        // Check and warn if business applications are running
                        string jobNames = string.Join(", ", selected.Select(j => j.Name));
                        await WaitForBusinessAppsToStop(vm, jobNames);
                        await vm.ExecuteRunSelected(selected, password);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Jobs blocked due to business application - already warned user
                    Debug.WriteLine($"[JobsPage] Jobs blocked: {nameof(OperationCanceledException)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JobsPage] Error in RunSelected: {ex.Message}");
                }
            };

            // Handle error dialogs
            vm.ErrorOccurred += (s, errorMsg) =>
            {
                try
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        var window = TopLevel.GetTopLevel(this) as Window;
                        var dialog = new ErrorDialog("Error", errorMsg);
                        await dialog.ShowDialog(window);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JobsPage] Error showing error dialog: {ex.Message}");
                }
            };
        }
    }
}
