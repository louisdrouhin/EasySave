namespace EasySave.Models;

// Available backup types
// Full: complete backup of all files
// Differential: backup only files modified since last backup
public enum JobType
{
    Full,
    Differential
}
