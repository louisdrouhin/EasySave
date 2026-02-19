using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Core;

public class StateTracker
{
  private readonly string _stateFilePath;
  private Dictionary<string, StateEntry> _jobStates = new();

  public StateTracker(string stateFilePath)
  {
    _stateFilePath = stateFilePath;
  }

  public void UpdateJobState(StateEntry stateEntry)
  {
    if (stateEntry == null || string.IsNullOrEmpty(stateEntry.JobName))
      return;

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

  public void RemoveJobState(int index)
  {
    if (index < 0 || index >= _jobStates.Count)
      return;

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
