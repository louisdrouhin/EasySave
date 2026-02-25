using Moq;

namespace EasyLog.Lib.Tests;

public class EasyLogTests
{
    private string GetTempLogBasePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EasyLogTests", Guid.NewGuid().ToString());
        return Path.Combine(tempDir, "test");
    }

    private void CleanupTestFile(string logBasePath)
    {
        var directory = Path.GetDirectoryName(logBasePath);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    private string GetActualLogFilePath(string logBasePath)
    {
        var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        return $"{logBasePath}_{dateStr}.json";
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logBasePath = GetTempLogBasePath();

        // Act
        var easyLog = new EasyLog(formatter.Object, logBasePath);

        // Assert
        Assert.NotNull(easyLog);
        // Verify that the path contains the date (e.g., test_2026-02-05.json)
        var currentPath = easyLog.GetCurrentLogPath();
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), currentPath);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Constructor_WithNullFormatter_ThrowsArgumentNullException()
    {
        // Arrange
        var logPath = GetTempLogBasePath();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EasyLog(null!, logPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPath_ThrowsArgumentException(string? invalidPath)
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new EasyLog(formatter.Object, invalidPath!));
    }

    [Fact]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var tempDir = Path.Combine(Path.GetTempPath(), "EasyLogTests", Guid.NewGuid().ToString());
        var logPath = Path.Combine(tempDir, "subdir", "test.log");

        // Act
        new EasyLog(formatter.Object, logPath);

        // Assert
        Assert.True(Directory.Exists(tempDir));

        // Cleanup
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_WithValidInput_WritesLogToFile()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);

        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestEvent";
        var content = new Dictionary<string, object> { { "key", "value" } };

        // Act
        easyLog.Write(timestamp, name, content);

        // Assert
        Assert.True(File.Exists(easyLog.GetCurrentLogPath()));
        var fileContent = File.ReadAllText(easyLog.GetCurrentLogPath());
        Assert.Contains("TestEvent", fileContent);
        Assert.Contains("value", fileContent);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Write_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter.Object, logPath);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            easyLog.Write(DateTime.Now, "TestEvent", null!));

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void Write_MultipleEntries_AppendsToFile()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);

        var content1 = new Dictionary<string, object> { { "entry", "first" } };
        var content2 = new Dictionary<string, object> { { "entry", "second" } };

        // Act
        easyLog.Write(DateTime.Now, "Event1", content1);
        easyLog.Write(DateTime.Now, "Event2", content2);

        // Assert
        var fileContent = File.ReadAllText(easyLog.GetCurrentLogPath());
        Assert.Contains("first", fileContent);
        Assert.Contains("second", fileContent);
        Assert.Contains("Event1", fileContent);
        Assert.Contains("Event2", fileContent);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Write_CallsFormatterWithCorrectParameters()
    {
        // Arrange
        var formatterMock = new Mock<ILogFormatter>();
        formatterMock.Setup(f => f.Format(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns("formatted log");

        var logPath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatterMock.Object, logPath);

        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestEvent";
        var content = new Dictionary<string, object> { { "key", "value" } };

        // Act
        easyLog.Write(timestamp, name, content);

        // Assert
        formatterMock.Verify(f => f.Format(timestamp, name, content), Times.Once);

        // Cleanup
        CleanupTestFile(logPath);
    }

    #endregion

    #region SetLogPath Tests

    [Fact]
    public void SetLogPath_WithValidPath_ChangesLogPath()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var initialBasePath = GetTempLogBasePath();
        var newBasePath = GetTempLogBasePath();

        var easyLog = new EasyLog(formatter.Object, initialBasePath);

        // Act
        easyLog.SetLogPath(newBasePath);

        // Assert
        var currentPath = easyLog.GetCurrentLogPath();
        Assert.StartsWith(newBasePath, currentPath);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), currentPath);

        // Cleanup
        CleanupTestFile(initialBasePath);
        CleanupTestFile(newBasePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetLogPath_WithInvalidPath_ThrowsArgumentException(string? invalidPath)
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter.Object, logPath);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            easyLog.SetLogPath(invalidPath!));

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void SetLogPath_CreatesNewDirectory()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var initialPath = GetTempLogBasePath();
        var tempDir = Path.Combine(Path.GetTempPath(), "EasyLogTests", Guid.NewGuid().ToString());
        var newPath = Path.Combine(tempDir, "newdir", "test.log");

        var easyLog = new EasyLog(formatter.Object, initialPath);

        // Act
        easyLog.SetLogPath(newPath);

        // Assert
        Assert.True(Directory.Exists(Path.GetDirectoryName(newPath)));

        // Cleanup
        CleanupTestFile(initialPath);
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SetLogPath_WritesToNewPath()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var initialBasePath = GetTempLogBasePath();
        var newBasePath = GetTempLogBasePath();

        var easyLog = new EasyLog(formatter, initialBasePath);
        easyLog.Write(DateTime.Now, "Event1", new Dictionary<string, object> { { "data", "initial" } });
        var initialFilePath = easyLog.GetCurrentLogPath();

        // Act
        easyLog.SetLogPath(newBasePath);
        easyLog.Write(DateTime.Now, "Event2", new Dictionary<string, object> { { "data", "new" } });
        var newFilePath = easyLog.GetCurrentLogPath();

        // Assert
        Assert.True(File.Exists(initialFilePath));
        Assert.True(File.Exists(newFilePath));

        var newFileContent = File.ReadAllText(newFilePath);
        Assert.Contains("Event2", newFileContent);
        Assert.DoesNotContain("Event1", newFileContent);

        // Cleanup
        CleanupTestFile(initialBasePath);
        CleanupTestFile(newBasePath);
    }

    #endregion

    #region GetCurrentLogPath Tests

    [Fact]
    public void GetCurrentLogPath_ReturnsCurrentPath()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logBasePath = GetTempLogBasePath();

        var easyLog = new EasyLog(formatter.Object, logBasePath);

        // Act
        var result = easyLog.GetCurrentLogPath();

        // Assert
        Assert.StartsWith(logBasePath, result);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), result);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void GetCurrentLogPath_AfterSetLogPath_ReturnsUpdatedPath()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var initialBasePath = GetTempLogBasePath();
        var newBasePath = GetTempLogBasePath();

        var easyLog = new EasyLog(formatter.Object, initialBasePath);
        easyLog.SetLogPath(newBasePath);

        // Act
        var result = easyLog.GetCurrentLogPath();

        // Assert
        Assert.StartsWith(newBasePath, result);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), result);

        // Cleanup
        CleanupTestFile(initialBasePath);
        CleanupTestFile(newBasePath);
    }

    #endregion

    #region JSON Structure Tests

    [Fact]
    public void Write_InitializesFileWithJsonStructure()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);

        // Act
        var fileContent = File.ReadAllText(easyLog.GetCurrentLogPath());

        // Assert
        Assert.StartsWith("{\"logs\":[", fileContent);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Close_ClosesJsonProperly()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);
        var content = new Dictionary<string, object> { { "test", "data" } };

        // Act
        easyLog.Write(DateTime.Now, "Event", content);
        easyLog.Close();

        // Assert
        var fileContent = File.ReadAllText(easyLog.GetCurrentLogPath());
        Assert.EndsWith("]}", fileContent);
        Assert.StartsWith("{\"logs\":[", fileContent);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Close_WithMultipleEntries_CreatesValidJson()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);

        // Act
        easyLog.Write(DateTime.Now, "Event1", new Dictionary<string, object> { { "data", "first" } });
        easyLog.Write(DateTime.Now, "Event2", new Dictionary<string, object> { { "data", "second" } });
        easyLog.Close();

        // Assert
        var fileContent = File.ReadAllText(easyLog.GetCurrentLogPath());

        // Verify that it is valid JSON
        var parsedJson = System.Text.Json.JsonDocument.Parse(fileContent);
        var logsArray = parsedJson.RootElement.GetProperty("logs");

        Assert.Equal(2, logsArray.GetArrayLength());

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    [Fact]
    public void Close_CalledMultipleTimes_DoesNotDuplicate()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var logBasePath = GetTempLogBasePath();
        var easyLog = new EasyLog(formatter, logBasePath);
        easyLog.Write(DateTime.Now, "Event", new Dictionary<string, object> { { "test", "data" } });

        // Act
        easyLog.Close();
        var contentAfterFirstClose = File.ReadAllText(easyLog.GetCurrentLogPath());
        easyLog.Close();
        var contentAfterSecondClose = File.ReadAllText(easyLog.GetCurrentLogPath());

        // Assert
        Assert.Equal(contentAfterFirstClose, contentAfterSecondClose);

        // Cleanup
        CleanupTestFile(logBasePath);
    }

    #endregion
}
