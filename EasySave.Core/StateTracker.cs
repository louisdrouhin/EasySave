using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Core;

// Manages and persists job state in JSON file
// Thread-safe synchronization of state updates
public class StateTracker
{
  private readonly string _stateFilePath;
  private readonly object _fileLock = new object();
  private Dictionary<string, StateEntry> _jobStates = new();
  public event EventHandler<StateEntry>? JobStateChanged;

  // Initializes StateTracker with state file path
  // @param stateFilePath - path to JSON file containing job states
  public StateTracker(string stateFilePath)
  {
    _stateFilePath = stateFilePath;
  }

  // Updates job state and persists to JSON file
  // @param stateEntry - state entry containing job name and state
  public void UpdateJobState(StateEntry stateEntry)
  {
    if (stateEntry == null || string.IsNullOrEmpty(stateEntry.JobName))
      return;

    lock (_fileLock)
    {
      _jobStates[stateEntry.JobName] = stateEntry;

      var options = new JsonSerializerOptions
      {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
      };
      var json = JsonSerializer.Serialize(_jobStates.Values, options);

      var directory = Path.GetDirectoryName(_stateFilePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllText(_stateFilePath, json);
    }

    JobStateChanged?.Invoke(this, stateEntry);
  }

  // Removes job state by index and persists
  // @param index - index of job to remove from collection
  public void RemoveJobState(int index)
  {
    if (index < 0 || index >= _jobStates.Count)
      return;

    lock (_fileLock)
    {
      var jobStateKey = _jobStates.Keys.ElementAt(index);

      if (_jobStates.Remove(jobStateKey))
      {
        var options = new JsonSerializerOptions
        {
          WriteIndented = true,
          Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(_jobStates.Values, options);
        File.WriteAllText(_stateFilePath, json);
      }
    }
  }
}
