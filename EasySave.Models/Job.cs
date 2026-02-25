namespace EasySave.Models;

// Represents a backup job
// Contains source, destination, and backup type information
public class Job
{
  public string Name { get; set; }
  public JobType Type { get; set; }
  public string SourcePath { get; set; }
  public string DestinationPath { get; set; }

  // Initializes a backup job
  // @param name - identifying name of the job
  // @param type - backup type (Full or Differential)
  // @param sourcePath - source directory path
  // @param destinationPath - destination directory path
  public Job(string name, JobType type, string sourcePath, string destinationPath)
  {
    Name = name;
    Type = type;
    SourcePath = sourcePath;
    DestinationPath = destinationPath;
  }

  // Returns a string representation of the job
  // @returns formatted string: "JobName (Type) : Source --> Destination"
  public override string ToString()
  {
    return $"{Name} ({Type}) : {SourcePath} --> {DestinationPath}";
  }
}
