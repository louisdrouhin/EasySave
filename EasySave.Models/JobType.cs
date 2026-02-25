namespace EasySave.Models;

// Types de sauvegarde disponibles
// Full: sauvegarde complète de tous les fichiers
// Differential: sauvegarde uniquement des fichiers modifiés depuis la dernière sauvegarde
public enum JobType
{
    Full,
    Differential
}
