namespace EasySave.Models;

// Représente l'état en temps réel d'une tâche de sauvegarde
// Contient informations basiques (nom, état, timestamp) et détails si Active
public class StateEntry
{
    public string JobName { get; set; }
    public DateTime LastActionTime { get; set; }
    public JobState State { get; set; }

    // Champs remplis uniquement quand State == Active
    public int? TotalFiles { get; set; }
    public long? TotalSizeToTransfer { get; set; }
    public double? Progress { get; set; }
    public int? RemainingFiles { get; set; }
    public long? RemainingSizeToTransfer { get; set; }
    public string? CurrentSourcePath { get; set; }
    public string? CurrentDestinationPath { get; set; }

    // Constructeur vide pour sérialisation JSON
    public StateEntry()
    {
        JobName = string.Empty;
    }

    // Crée un StateEntry avec infos basiques (pour état Inactive)
    // @param jobName - nom du job
    // @param lastActionTime - date/heure de la dernière action
    // @param state - état actuel du job
    public StateEntry(string jobName, DateTime lastActionTime, JobState state)
    {
        JobName = jobName;
        LastActionTime = lastActionTime;
        State = state;
    }

    // Crée un StateEntry complet avec progression (pour état Active)
    // @param jobName - nom du job
    // @param lastActionTime - date/heure de la dernière action
    // @param state - état du job
    // @param totalFiles - nombre total de fichiers à traiter
    // @param totalSizeToTransfer - taille totale en bytes
    // @param progress - pourcentage de progression (0-100)
    // @param remainingFiles - fichiers restants à traiter
    // @param remainingSizeToTransfer - taille restante en bytes
    // @param currentSourcePath - chemin du fichier en cours de traitement
    // @param currentDestinationPath - chemin de destination du fichier en cours
    public StateEntry(
        string jobName,
        DateTime lastActionTime,
        JobState state,
        int totalFiles = 0,
        long totalSizeToTransfer = 0,
        double progress = 0,
        int remainingFiles = 0,
        long remainingSizeToTransfer = 0,
        string currentSourcePath = "",
        string currentDestinationPath = "")
    {
        JobName = jobName;
        LastActionTime = lastActionTime;
        State = state;
        TotalFiles = totalFiles;
        TotalSizeToTransfer = totalSizeToTransfer;
        Progress = progress;
        RemainingFiles = remainingFiles;
        RemainingSizeToTransfer = remainingSizeToTransfer;
        CurrentSourcePath = currentSourcePath;
        CurrentDestinationPath = currentDestinationPath;
    }

    // Retourne une représentation textuelle de l'état du job
    // Inclut la progression si le job est en cours d'exécution
    // @returns chaîne formatée avec toutes les informations pertinentes
    public override string ToString()
    {
        var baseInfo = $"JobName={JobName}, LastActionTime={LastActionTime:yyyy-MM-dd HH:mm:ss.fff}, State={State}";

        if (State == JobState.Inactive)
        {
            return baseInfo;
        }

        return $"{baseInfo}, TotalFiles={TotalFiles}, TotalSizeToTransfer={TotalSizeToTransfer}, Progress={Progress}%, RemainingFiles={RemainingFiles}, RemainingSizeToTransfer={RemainingSizeToTransfer}, CurrentSourcePath={CurrentSourcePath}, CurrentDestinationPath={CurrentDestinationPath}";
    }
}
