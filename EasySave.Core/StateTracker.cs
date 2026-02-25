using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Core;

// Gère et persiste l'état des jobs dans un fichier JSON
// Synchronisation thread-safe des mises à jour d'état
public class StateTracker
{
  private readonly string _stateFilePath;
  private readonly object _fileLock = new object();
  private Dictionary<string, StateEntry> _jobStates = new();
  public event EventHandler<StateEntry>? JobStateChanged;

  // Initialise le StateTracker avec le chemin du fichier d'état
  // @param stateFilePath - chemin du fichier JSON contenant les états des jobs
  public StateTracker(string stateFilePath)
  {
    _stateFilePath = stateFilePath;
  }

  // Met à jour l'état d'un job et le persiste dans le fichier JSON
  // @param stateEntry - entrée d'état contenant le nom et l'état du job
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

  // Supprime l'état d'un job par index et le persiste
  // @param index - index du job à supprimer dans la collection
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
