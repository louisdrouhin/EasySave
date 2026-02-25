namespace EasySave.Models;

// États possibles d'une tâche de sauvegarde
// Active: actuellement en cours d'exécution
// Inactive: arrêtée ou jamais lancée
// Paused: en cours mais suspendue
public enum JobState
{
    Active,
    Inactive,
    Paused
}
