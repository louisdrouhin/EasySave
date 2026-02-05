namespace EasySave.Models.Tests;

public class LogEntryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SucceedsAndSetsProperties()
    {
        // Arrange
        var timestamp = new DateTime(2025, 2, 5, 10, 30, 45, 123);
        var backupName = "Daily Backup";
        var sourcePath = @"\\server\source\file.txt";
        var destinationPath = @"\\backup\destination\file.txt";
        var fileSize = 5242880L; // 5MB
        var transferTimeMs = 2500;

        // Act
        var logEntry = new LogEntry(timestamp, backupName, sourcePath, destinationPath, fileSize, transferTimeMs);

        // Assert
        Assert.Equal(timestamp, logEntry.Timestamp);
        Assert.Equal(backupName, logEntry.BackupName);
        Assert.Equal(sourcePath, logEntry.SourcePath);
        Assert.Equal(destinationPath, logEntry.DestinationPath);
        Assert.Equal(fileSize, logEntry.FileSize);
        Assert.Equal(transferTimeMs, logEntry.TransferTimeMs);
    }

    [Fact]
    public void Constructor_WithNegativeTransferTime_Succeeds()
    {
        // Arrange
        var timestamp = new DateTime(2025, 2, 5, 10, 30, 45);
        var transferTimeMs = -1; // Error case

        // Act
        var logEntry = new LogEntry(
            timestamp,
            "Backup",
            @"\\source\file.txt",
            @"\\dest\file.txt",
            1024,
            transferTimeMs);

        // Assert
        Assert.Equal(-1, logEntry.TransferTimeMs);
    }

    [Fact]
    public void Constructor_WithZeroFileSize_Succeeds()
    {
        // Arrange & Act
        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            @"\\source\file.txt",
            @"\\dest\file.txt",
            0,
            100);

        // Assert
        Assert.Equal(0, logEntry.FileSize);
    }

    #endregion

    #region ToNormalizedFormat Tests

    [Fact]
    public void ToNormalizedFormat_ReturnsCorrectTuple()
    {
        // Arrange
        var timestamp = new DateTime(2025, 2, 5, 10, 30, 45);
        var backupName = "Test Backup";
        var sourcePath = @"\\server\source\documents\file.docx";
        var destinationPath = @"\\backup\destination\documents\file.docx";
        var fileSize = 2097152L; // 2MB
        var transferTimeMs = 1500;

        var logEntry = new LogEntry(timestamp, backupName, sourcePath, destinationPath, fileSize, transferTimeMs);

        // Act
        var (returnedTimestamp, returnedName, content) = logEntry.ToNormalizedFormat();

        // Assert
        Assert.Equal(timestamp, returnedTimestamp);
        Assert.Equal(backupName, returnedName);
        Assert.NotNull(content);
        Assert.Equal(4, content.Count);
    }

    [Fact]
    public void ToNormalizedFormat_ContainsAllRequiredKeys()
    {
        // Arrange
        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            @"\\source\file.txt",
            @"\\dest\file.txt",
            1024,
            100);

        // Act
        var (_, _, content) = logEntry.ToNormalizedFormat();

        // Assert
        Assert.Contains("sourcePath", content.Keys);
        Assert.Contains("destinationPath", content.Keys);
        Assert.Contains("fileSize", content.Keys);
        Assert.Contains("transferTimeMs", content.Keys);
    }

    [Fact]
    public void ToNormalizedFormat_ContentValuesAreCorrect()
    {
        // Arrange
        var sourcePath = @"\\server\source\file.txt";
        var destinationPath = @"\\backup\destination\file.txt";
        var fileSize = 5242880L;
        var transferTimeMs = 2500;

        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            sourcePath,
            destinationPath,
            fileSize,
            transferTimeMs);

        // Act
        var (_, _, content) = logEntry.ToNormalizedFormat();

        // Assert
        Assert.Equal(sourcePath, content["sourcePath"]);
        Assert.Equal(destinationPath, content["destinationPath"]);
        Assert.Equal(fileSize, content["fileSize"]);
        Assert.Equal(transferTimeMs, content["transferTimeMs"]);
    }

    [Fact]
    public void ToNormalizedFormat_WithErrorTransferTime_IncludesNegativeValue()
    {
        // Arrange
        var errorTime = -1;
        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            @"\\source\file.txt",
            @"\\dest\file.txt",
            1024,
            errorTime);

        // Act
        var (_, _, content) = logEntry.ToNormalizedFormat();

        // Assert
        Assert.Equal(errorTime, content["transferTimeMs"]);
    }

    #endregion

    #region UNC Path Tests

    [Theory]
    [InlineData(@"\\server\share\path\file.txt")]
    [InlineData(@"\\192.168.1.1\backup\file.doc")]
    [InlineData(@"\\localhost\shared\documents\file.xlsx")]
    public void Constructor_AcceptsValidUNCPaths(string uncPath)
    {
        // Arrange & Act
        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            uncPath,
            uncPath,
            1024,
            100);

        // Assert
        Assert.Equal(uncPath, logEntry.SourcePath);
        Assert.Equal(uncPath, logEntry.DestinationPath);
    }

    #endregion

    #region Large File Size Tests

    [Fact]
    public void Constructor_WithLargeFileSize_Succeeds()
    {
        // Arrange
        var largeFileSize = 1099511627776L; // 1TB

        // Act
        var logEntry = new LogEntry(
            DateTime.Now,
            "Backup",
            @"\\source\large.iso",
            @"\\dest\large.iso",
            largeFileSize,
            50000);

        // Assert
        Assert.Equal(largeFileSize, logEntry.FileSize);
    }

    #endregion
}
