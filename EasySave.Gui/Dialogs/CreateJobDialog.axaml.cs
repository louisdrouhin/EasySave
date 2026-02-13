using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasySave.Core.Localization;
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
        
        Title = LocalizationManager.Get("CreateJobDialog_Title");

        var jobNameLabel = this.FindControl<TextBlock>("JobNameLabel");
        if (jobNameLabel != null) jobNameLabel.Text = LocalizationManager.Get("CreateJobDialog_JobName");

        var jobNameInput = this.FindControl<TextBox>("JobNameInput");
        if (jobNameInput != null) jobNameInput.Watermark = LocalizationManager.Get("CreateJobDialog_JobName_Placeholder");

        var jobTypeLabel = this.FindControl<TextBlock>("JobTypeLabel");
        if (jobTypeLabel != null) jobTypeLabel.Text = LocalizationManager.Get("CreateJobDialog_JobType");

        var sourcePathLabel = this.FindControl<TextBlock>("SourcePathLabel");
        if (sourcePathLabel != null) sourcePathLabel.Text = LocalizationManager.Get("CreateJobDialog_SourcePath");

        var sourcePathInput = this.FindControl<TextBox>("SourcePathInput");
        if (sourcePathInput != null) sourcePathInput.Watermark = LocalizationManager.Get("CreateJobDialog_SourcePath_Placeholder");

        var destPathLabel = this.FindControl<TextBlock>("DestPathLabel");
        if (destPathLabel != null) destPathLabel.Text = LocalizationManager.Get("CreateJobDialog_DestPath");
        
        var destinationPathInput = this.FindControl<TextBox>("DestinationPathInput");
        if (destinationPathInput != null) destinationPathInput.Watermark = LocalizationManager.Get("CreateJobDialog_DestPath_Placeholder");

        var createButton = this.FindControl<Button>("CreateButton");
        if (createButton != null)
        {
            createButton.Content = LocalizationManager.Get("CreateJobDialog_Button_Create");
            createButton.Click += CreateButton_Click;
        }

        var cancelButton = this.FindControl<Button>("CancelButton");
        if (cancelButton != null)
        {
            cancelButton.Content = LocalizationManager.Get("CreateJobDialog_Button_Cancel");
            cancelButton.Click += CancelButton_Click;
        }
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

        var jobTypeInput = this.FindControl<ComboBox>("JobTypeInput");
        if (jobTypeInput != null)
        {
            jobTypeInput.Items.Clear();
            jobTypeInput.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("CreateJobDialog_JobType_Full"), Tag = JobType.Full });
            jobTypeInput.Items.Add(new ComboBoxItem { Content = LocalizationManager.Get("CreateJobDialog_JobType_Differential"), Tag = JobType.Differential });
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
                        Title = LocalizationManager.Get("CreateJobDialog_SelectSource"),
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
            catch (Exception)
            {
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
                        Title = LocalizationManager.Get("CreateJobDialog_SelectDest"),
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
            catch (Exception)
            {
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
            // Ideally show error dialog, but for now just return or console log if allowed (but removed per instruction)
            // Consider adding a small error textblock to UI if needed, but for now I'll just skip.
            return;
        }

        JobType jobType = JobType.Full;
        if (jobTypeInput?.SelectedItem is ComboBoxItem selectedItem)
        {
            if (selectedItem.Tag is JobType type)
            {
                jobType = type;
            }
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
