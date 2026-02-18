using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Core;

public class StateTracker
{
  private readonly string _stateFilePath;
  private Dictionary<string, StateEntry> _jobStates = new();
  private readonly object _lock = new object();

  public StateTracker(string stateFilePath)
  {
    // Convertir en chemin absolu
    if (!Path.IsPathRooted(stateFilePath))
    {
      _stateFilePath = Path.Combine(AppContext.BaseDirectory, stateFilePath);
    }
    else
    {
      _stateFilePath = stateFilePath;
    }

    // Normaliser le chemin
    _stateFilePath = Path.GetFullPath(_stateFilePath);

    System.Diagnostics.Debug.WriteLine($"[StateTracker] State file path: {_stateFilePath}");
  }

  public void UpdateJobState(StateEntry stateEntry)
  {
    if (stateEntry == null || string.IsNullOrEmpty(stateEntry.JobName))
      return;

    lock (_lock)
    {
      _jobStates[stateEntry.JobName] = stateEntry;

      System.Diagnostics.Debug.WriteLine($"[StateTracker] Updating state for '{stateEntry.JobName}': {stateEntry.State}, Progress: {stateEntry.Progress:F1}%");

      try
      {
        var options = new JsonSerializerOptions
        {
          WriteIndented = true,
          Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(_jobStates.Values, options);

        var directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          System.Diagnostics.Debug.WriteLine($"[StateTracker] Creating directory: {directory}");
          Directory.CreateDirectory(directory);
        }

        // Écriture atomique avec FileShare pour permettre la lecture simultanée
        using (var fs = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var sw = new StreamWriter(fs))
        {
          sw.Write(json);
          sw.Flush();
          fs.Flush(true);
        }

        System.Diagnostics.Debug.WriteLine($"[StateTracker] State written to: {_stateFilePath} ({json.Length} chars)");
      }
      catch (IOException ex)
      {
        // Log l'erreur mais ne bloque pas l'exécution
        System.Diagnostics.Debug.WriteLine($"[StateTracker] Error writing state: {ex.Message}");
      }
    }
  }

  public StateEntry? GetJobState(string jobName)
  {
    if (string.IsNullOrEmpty(jobName))
      return null;

    lock (_lock)
    {
      if (_jobStates.TryGetValue(jobName, out var state))
      {
        return state;
      }
      return null;
    }
  }

  public void RemoveJobState(int index)
  {
    lock (_lock)
    {
      if (index < 0 || index >= _jobStates.Count)
        return;

      var jobStateKey = _jobStates.Keys.ElementAt(index);

      if (_jobStates.Remove(jobStateKey))
      {
        try
        {
          var options = new JsonSerializerOptions
          {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
          };
          var json = JsonSerializer.Serialize(_jobStates.Values, options);

          // Écriture atomique avec FileShare pour permettre la lecture simultanée
          using (var fs = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
          using (var sw = new StreamWriter(fs))
          {
            sw.Write(json);
            sw.Flush();
            fs.Flush(true);
          }
        }
        catch (IOException ex)
        {
          // Log l'erreur mais ne bloque pas l'exécution
          System.Diagnostics.Debug.WriteLine($"[StateTracker] Error removing state: {ex.Message}");
        }
      }
    }
  }
}
