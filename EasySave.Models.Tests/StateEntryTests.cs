namespace EasySave.Models.Tests;

public class StateEntryTests
{
    #region Inactive State Tests

    [Fact]
    public void Constructor_InactiveJob_CreatesEntryWithoutActiveProperties()
    {
        // Arrange
        var jobName = "Weekly Backup";
        var timestamp = new DateTime(2025, 2, 5, 10, 30, 45);

        // Act
        var stateEntry = new StateEntry(jobName, timestamp, JobState.Inactive);

        // Assert
        Assert.Equal(jobName, stateEntry.JobName);
        Assert.Equal(timestamp, stateEntry.LastActionTime);
        Assert.Equal(JobState.Inactive, stateEntry.State);
        Assert.Null(stateEntry.TotalFiles);
        Assert.Null(stateEntry.TotalSizeToTransfer);
        Assert.Null(stateEntry.Progress);
        Assert.Null(stateEntry.RemainingFiles);
        Assert.Null(stateEntry.RemainingSizeToTransfer);
        Assert.Null(stateEntry.CurrentSourcePath);
        Assert.Null(stateEntry.CurrentDestinationPath);
    }

    [Fact]
    public void ToString_InactiveJob_ShowsOnlyBasicInfo()
    {
        // Arrange
        var stateEntry = new StateEntry("Backup Job", DateTime.Now, JobState.Inactive);

        // Act
        var result = stateEntry.ToString();

        // Assert
        Assert.Contains("JobName=Backup Job", result);
        Assert.Contains("State=Inactive", result);
        Assert.DoesNotContain("TotalFiles", result);
        Assert.DoesNotContain("Progress", result);
    }

    #endregion

    #region Active State Tests

    [Fact]
    public void Constructor_ActiveJob_CreatesEntryWithAllProperties()
    {
        // Arrange
        var jobName = "Daily Backup";
        var timestamp = new DateTime(2025, 2, 5, 10, 30, 45);
        var totalFiles = 150;
        var totalSizeToTransfer = 52428800L; // 50MB
        var progress = 45.5;
        var remainingFiles = 82;
        var remainingSizeToTransfer = 28835840L;
        var sourcePath = @"\\server\source\documents\file.docx";
        var destinationPath = @"\\backup\destination\documents\file.docx";

        // Act
        var stateEntry = new StateEntry(
            jobName,
            timestamp,
            JobState.Active,
            totalFiles,
            totalSizeToTransfer,
            progress,
            remainingFiles,
            remainingSizeToTransfer,
            sourcePath,
            destinationPath);

        // Assert
        Assert.Equal(jobName, stateEntry.JobName);
        Assert.Equal(timestamp, stateEntry.LastActionTime);
        Assert.Equal(JobState.Active, stateEntry.State);
        Assert.Equal(totalFiles, stateEntry.TotalFiles);
        Assert.Equal(totalSizeToTransfer, stateEntry.TotalSizeToTransfer);
        Assert.Equal(progress, stateEntry.Progress);
        Assert.Equal(remainingFiles, stateEntry.RemainingFiles);
        Assert.Equal(remainingSizeToTransfer, stateEntry.RemainingSizeToTransfer);
        Assert.Equal(sourcePath, stateEntry.CurrentSourcePath);
        Assert.Equal(destinationPath, stateEntry.CurrentDestinationPath);
    }

    [Fact]
    public void ToString_ActiveJob_ShowsAllDetails()
    {
        // Arrange
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            10485760L,
            50.0,
            50,
            5242880L,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Act
        var result = stateEntry.ToString();

        // Assert
        Assert.Contains("JobName=Backup", result);
        Assert.Contains("State=Active", result);
        Assert.Contains("TotalFiles=100", result);
        Assert.Contains("Progress=50%", result);
        Assert.Contains("RemainingFiles=50", result);
    }

    [Fact]
    public void ToString_ActiveJob_IncludesCurrentPaths()
    {
        // Arrange
        var sourcePath = @"\\server\source\important\file.exe";
        var destinationPath = @"\\backup\destination\important\file.exe";

        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            1048576,
            25.0,
            75,
            786432,
            sourcePath,
            destinationPath);

        // Act
        var result = stateEntry.ToString();

        // Assert
        Assert.Contains(sourcePath, result);
        Assert.Contains(destinationPath, result);
    }

    #endregion

    #region Progress Tests

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.5)]
    [InlineData(99.9)]
    [InlineData(100.0)]
    public void Constructor_WithVariousProgressValues_Succeeds(double progress)
    {
        // Arrange & Act
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            1048576,
            progress,
            50,
            524288,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Assert
        Assert.Equal(progress, stateEntry.Progress);
    }

    #endregion

    #region File Count Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(999999)]
    public void Constructor_WithVariousFileCountValues_Succeeds(int fileCount)
    {
        // Arrange & Act
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            fileCount,
            1048576,
            50.0,
            fileCount / 2,
            524288,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Assert
        Assert.Equal(fileCount, stateEntry.TotalFiles);
    }

    #endregion

    #region Size Tests

    [Theory]
    [InlineData(0L)]
    [InlineData(1024L)]
    [InlineData(1048576L)] // 1MB
    [InlineData(1099511627776L)] // 1TB
    public void Constructor_WithVariousFileSizes_Succeeds(long size)
    {
        // Arrange & Act
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            size,
            50.0,
            50,
            size / 2,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Assert
        Assert.Equal(size, stateEntry.TotalSizeToTransfer);
        Assert.Equal(size / 2, stateEntry.RemainingSizeToTransfer);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void CanChangeFromInactiveToActive()
    {
        // Arrange
        var stateEntry = new StateEntry("Backup", DateTime.Now, JobState.Inactive);

        // Act
        stateEntry.State = JobState.Active;
        stateEntry.TotalFiles = 100;
        stateEntry.TotalSizeToTransfer = 1048576;
        stateEntry.Progress = 0;
        stateEntry.RemainingFiles = 100;
        stateEntry.RemainingSizeToTransfer = 1048576;
        stateEntry.CurrentSourcePath = @"\\source\file.txt";
        stateEntry.CurrentDestinationPath = @"\\dest\file.txt";

        // Assert
        Assert.Equal(JobState.Active, stateEntry.State);
        Assert.NotNull(stateEntry.TotalFiles);
    }

    [Fact]
    public void CanChangeFromActiveToInactive()
    {
        // Arrange
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            1048576,
            100.0,
            0,
            0,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Act
        stateEntry.State = JobState.Inactive;

        // Assert
        Assert.Equal(JobState.Inactive, stateEntry.State);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Constructor_PreservesTimestampAccuracy()
    {
        // Arrange
        var timestamp = new DateTime(2025, 2, 5, 14, 23, 45, 123);

        // Act
        var stateEntry = new StateEntry("Backup", timestamp, JobState.Inactive);

        // Assert
        Assert.Equal(timestamp, stateEntry.LastActionTime);
    }

    #endregion

    #region ToString Format Tests

    [Fact]
    public void ToString_IncludesTimestampInCorrectFormat()
    {
        // Arrange
        var timestamp = new DateTime(2025, 2, 5, 14, 23, 45, 123);
        var stateEntry = new StateEntry("Backup", timestamp, JobState.Inactive);

        // Act
        var result = stateEntry.ToString();

        // Assert
        Assert.Contains("2025-02-05 14:23:45.123", result);
    }

    [Fact]
    public void ToString_ActiveJobShowsProgressWithPercentSign()
    {
        // Arrange
        var stateEntry = new StateEntry(
            "Backup",
            DateTime.Now,
            JobState.Active,
            100,
            1048576,
            50.0,
            67,
            698579,
            @"\\source\file.txt",
            @"\\dest\file.txt");

        // Act
        var result = stateEntry.ToString();

        // Assert
        Assert.Contains("Progress=50%", result);
    }

    #endregion
}
