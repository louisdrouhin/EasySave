using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasySave.Models;
using System;
using System.Threading.Tasks;

namespace EasySave.GUI.Dialogs;

public partial class CreateJobDialog : Window
{
    public class JobResult
    {
        public string Name { get; set; } = "";
        public JobType Type { get; set; } = JobType.Full;
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
    }

    private JobResult? _result = null;

    public CreateJobDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var browseSourceButton = this.FindControl<Button>("BrowseSourceButton");
        if (browseSourceButton != null)
        {
            browseSourceButton.Click += BrowseSourceButton_Click;
        }

        var browseDestinationButton = this.FindControl<Button>("BrowseDestinationButton");
        if (browseDestinationButton != null)
        {
            browseDestinationButton.Click += BrowseDestinationButton_Click;
        }

        var createButton = this.FindControl<Button>("CreateButton");
        if (createButton != null)
        {
            createButton.Click += CreateButton_Click;
        }

        var cancelButton = this.FindControl<Button>("CancelButton");
        if (cancelButton != null)
        {
            cancelButton.Click += CancelButton_Click;
        }

        var jobTypeInput = this.FindControl<ComboBox>("JobTypeInput");
        if (jobTypeInput != null)
        {
            jobTypeInput.SelectedIndex = 0;
        }
    }

    private async void BrowseSourceButton_Click(object? sender, RoutedEventArgs e)
    {
        var sourcePathInput = this.FindControl<TextBox>("SourcePathInput");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider != null)
        {
            try
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Select Source Folder",
                        AllowMultiple = false
                    });

                if (folders.Count > 0)
                {
                    var selectedPath = folders[0].Path.LocalPath;
                    if (sourcePathInput != null)
                    {
                        sourcePathInput.Text = selectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error browsing source folder: {ex.Message}");
            }
        }
    }

    private async void BrowseDestinationButton_Click(object? sender, RoutedEventArgs e)
    {
        var destinationPathInput = this.FindControl<TextBox>("DestinationPathInput");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider != null)
        {
            try
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Select Destination Folder",
                        AllowMultiple = false
                    });

                if (folders.Count > 0)
                {
                    var selectedPath = folders[0].Path.LocalPath;
                    if (destinationPathInput != null)
                    {
                        destinationPathInput.Text = selectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error browsing destination folder: {ex.Message}");
            }
        }
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e)
    {
        var jobNameInput = this.FindControl<TextBox>("JobNameInput");
        var jobTypeInput = this.FindControl<ComboBox>("JobTypeInput");
        var sourcePathInput = this.FindControl<TextBox>("SourcePathInput");
        var destinationPathInput = this.FindControl<TextBox>("DestinationPathInput");

        string name = jobNameInput?.Text ?? "";
        string sourcePath = sourcePathInput?.Text ?? "";
        string destinationPath = destinationPathInput?.Text ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
        {
            Console.WriteLine("Please fill in all fields");
            return;
        }

        JobType jobType = JobType.Full;
        if (jobTypeInput?.SelectedItem is ComboBoxItem selectedItem)
        {
            string typeStr = selectedItem.Content?.ToString() ?? "Full";
            jobType = typeStr.ToLower() == "differential" ? JobType.Differential : JobType.Full;
        }

        _result = new JobResult
        {
            Name = name,
            Type = jobType,
            SourcePath = sourcePath,
            DestinationPath = destinationPath
        };

        Close(_result);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
