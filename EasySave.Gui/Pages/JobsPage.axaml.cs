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
    public JobsPage()
    {
        InitializeComponent();
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
                    // Check for business applications (just log, don't show warning)
                    string? businessApp = vm.CheckBusinessApp();
                    if (!string.IsNullOrEmpty(businessApp))
                    {
                        Debug.WriteLine($"[JobsPage] Business app running: {businessApp}");
                    }

                    // Show password dialog
                    var window = TopLevel.GetTopLevel(this) as Window;
                    var passwordDialog = new PasswordDialog();
                    var password = await passwordDialog.ShowDialog<string?>(window);

                    if (password != null)
                    {
                        await vm.ExecutePlayJob(jobVm, password);
                    }
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

                    // Check for business applications (just log, don't show warning)
                    string? businessApp = vm.CheckBusinessApp();
                    if (!string.IsNullOrEmpty(businessApp))
                    {
                        Debug.WriteLine($"[JobsPage] Business app running: {businessApp}");
                    }

                    // Show password dialog
                    var window = TopLevel.GetTopLevel(this) as Window;
                    var passwordDialog = new PasswordDialog();
                    var password = await passwordDialog.ShowDialog<string?>(window);

                    if (password != null)
                    {
                        await vm.ExecuteRunSelected(selected, password);
                    }
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
