using System.Text.Json;
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

    // Mise à jour de l'état en mémoire
    _jobStates[stateEntry.JobName] = stateEntry;

    // Sérialisation et écriture dans le fichier
    var options = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(_jobStates.Values, options);

    // S'assurer que le dossier existe
    var directory = Path.GetDirectoryName(_stateFilePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    File.WriteAllText(_stateFilePath, json);
  }

  public void RemoveJobState(string jobName)
  {
    if (string.IsNullOrEmpty(jobName))
      return;
    // Suppression de l'état en mémoire
    if (_jobStates.Remove(jobName))
    {
      // Mise à jour du fichier après suppression
      var options = new JsonSerializerOptions { WriteIndented = true };
      var json = JsonSerializer.Serialize(_jobStates.Values, options);
      File.WriteAllText(_stateFilePath, json);
    }
  }
}
