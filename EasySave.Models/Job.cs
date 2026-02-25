namespace EasySave.Models;

// Représente une tâche de sauvegarde (backup job)
// Contient les informations de source, destination et type de sauvegarde
public class Job
{
  public string Name { get; set; }
  public JobType Type { get; set; }
  public string SourcePath { get; set; }
  public string DestinationPath { get; set; }

  // Initialise un job de sauvegarde
  // @param name - nom identifiant du job
  // @param type - type de sauvegarde (Full ou Differential)
  // @param sourcePath - chemin du répertoire source
  // @param destinationPath - chemin du répertoire destination
  public Job(string name, JobType type, string sourcePath, string destinationPath)
  {
    Name = name;
    Type = type;
    SourcePath = sourcePath;
    DestinationPath = destinationPath;
  }

  // Retourne une représentation textuelle du job
  // @returns chaîne formatée: "NomJob (Type) : Source --> Destination"
  public override string ToString()
  {
    return $"{Name} ({Type}) : {SourcePath} --> {DestinationPath}";
  }
}
