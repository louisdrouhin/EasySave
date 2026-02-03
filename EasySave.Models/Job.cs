namespace EasySave.Models;

public class Job
{
  public string Name { get; set; }
  public JobType Type { get; set; }
  public string SourcePath { get; set; }
  public string DestinationPath { get; set; }
  
  public Job(string name, JobType type, string sourcePath, string destinationPath)
  {
    Name = name;
    Type = type;
    SourcePath = sourcePath;
    DestinationPath = destinationPath;
  }
}
