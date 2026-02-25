namespace EasySave.Models;

// Possible states of a backup job
// Active: currently executing
// Inactive: stopped or never started
// Paused: in progress but suspended
public enum JobState
{
    Active,
    Inactive,
    Paused
}
