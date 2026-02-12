using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Core;
using EasySave.GUI.Components;
using EasySave.Models;

namespace EasySave.GUI.Pages
{
  public partial class StatePage : UserControl
  {
    private FileSystemWatcher? _watcher;
    private string _stateFilePath = string.Empty;
    private StackPanel? _statesStackPanel;
    private TextBlock? _errorMessageTextBlock;
    private readonly JobManager? _jobManager;

    public StatePage()
    {
      InitializeComponent();
    }

    public StatePage(JobManager jobManager) : this()
    {
        _jobManager = jobManager;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
      base.OnAttachedToVisualTree(e);
      _statesStackPanel = this.FindControl<StackPanel>("StatesStackPanel");
      _errorMessageTextBlock = this.FindControl<TextBlock>("ErrorMessageTextBlock");
      InitializeStateWatcher();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
      base.OnDetachedFromVisualTree(e);
      _watcher?.Dispose();
      _watcher = null;
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
            
            if (File.Exists(executionDirState))
            {
                _stateFilePath = executionDirState;
            }
            else if (File.Exists(projectRootState))
            {
                _stateFilePath = Path.GetFullPath(projectRootState);
            }
        }

        UpdateStateContent();

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
      }
      catch (Exception ex)
      {
         ShowError($"Error initializing state watcher: {ex.Message}");
      }
    }

    private void OnStateFileChanged(object sender, FileSystemEventArgs e)
    {
      Dispatcher.UIThread.Post(UpdateStateContent);
    }

    private void ShowError(string message)
    {
        if (_errorMessageTextBlock != null)
        {
            _errorMessageTextBlock.Text = message;
            _errorMessageTextBlock.IsVisible = true;
        }
    }

    private void HideError()
    {
        if (_errorMessageTextBlock != null)
        {
            _errorMessageTextBlock.IsVisible = false;
        }
    }

    private void UpdateStateContent()
    {
      if (_statesStackPanel == null)
      {
          _statesStackPanel = this.FindControl<StackPanel>("StatesStackPanel");
      }

      if (_statesStackPanel == null) return;

      try
      {
        if (!File.Exists(_stateFilePath))
        {
          ShowError($"State file not found at: {_stateFilePath}");
          _statesStackPanel.Children.Clear();
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
                _statesStackPanel.Children.Clear();
                foreach (var state in states)
                {
                    var card = new StateCard(state);
                    _statesStackPanel.Children.Add(card);
                }
                HideError();
            }
        }
      }
      catch (Exception)
      {
      }
    }
  }
}
