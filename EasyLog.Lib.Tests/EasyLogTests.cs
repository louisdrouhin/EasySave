using Moq;

namespace EasyLog.Lib.Tests;

public class EasyLogTests
{
    private string GetTempLogPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "EasyLogTests", Guid.NewGuid().ToString());
        return Path.Combine(tempDir, "test.log");
    }

    private void CleanupTestFile(string logPath)
    {
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        var directory = Path.GetDirectoryName(logPath);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogPath();

        // Act
        var easyLog = new EasyLog(formatter.Object, logPath);

        // Assert
        Assert.NotNull(easyLog);
        Assert.Equal(logPath, easyLog.GetCurrentLogPath());

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void Constructor_WithNullFormatter_ThrowsArgumentNullException()
    {
        // Arrange
        var logPath = GetTempLogPath();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EasyLog(null!, logPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPath_ThrowsArgumentException(string invalidPath)
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new EasyLog(formatter.Object, invalidPath));
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
        var logPath = GetTempLogPath();
        var easyLog = new EasyLog(formatter, logPath);

        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestEvent";
        var content = new Dictionary<string, object> { { "key", "value" } };

        // Act
        easyLog.Write(timestamp, name, content);

        // Assert
        Assert.True(File.Exists(logPath));
        var fileContent = File.ReadAllText(logPath);
        Assert.Contains("TestEvent", fileContent);
        Assert.Contains("value", fileContent);

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void Write_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogPath();
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
        var logPath = GetTempLogPath();
        var easyLog = new EasyLog(formatter, logPath);

        var content1 = new Dictionary<string, object> { { "entry", "first" } };
        var content2 = new Dictionary<string, object> { { "entry", "second" } };

        // Act
        easyLog.Write(DateTime.Now, "Event1", content1);
        easyLog.Write(DateTime.Now, "Event2", content2);

        // Assert
        var fileContent = File.ReadAllText(logPath);
        var lines = fileContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void Write_CallsFormatterWithCorrectParameters()
    {
        // Arrange
        var formatterMock = new Mock<ILogFormatter>();
        formatterMock.Setup(f => f.Format(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns("formatted log");

        var logPath = GetTempLogPath();
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

    [Fact]
    public void Write_WithFormatterException_PropagatesException()
    {
        // Arrange
        var formatterMock = new Mock<ILogFormatter>();
        formatterMock.Setup(f => f.Format(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Throws(new InvalidOperationException("Formatter error"));

        var logPath = GetTempLogPath();
        var easyLog = new EasyLog(formatterMock.Object, logPath);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            easyLog.Write(DateTime.Now, "Event", new Dictionary<string, object>()));

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
        var initialPath = GetTempLogPath();
        var newPath = GetTempLogPath();

        var easyLog = new EasyLog(formatter.Object, initialPath);

        // Act
        easyLog.SetLogPath(newPath);

        // Assert
        Assert.Equal(newPath, easyLog.GetCurrentLogPath());

        // Cleanup
        CleanupTestFile(initialPath);
        CleanupTestFile(newPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetLogPath_WithInvalidPath_ThrowsArgumentException(string invalidPath)
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogPath();
        var easyLog = new EasyLog(formatter.Object, logPath);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            easyLog.SetLogPath(invalidPath));

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void SetLogPath_CreatesNewDirectory()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var initialPath = GetTempLogPath();
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
        var initialPath = GetTempLogPath();
        var newPath = GetTempLogPath();

        var easyLog = new EasyLog(formatter, initialPath);
        easyLog.Write(DateTime.Now, "Event1", new Dictionary<string, object> { { "data", "initial" } });

        // Act
        easyLog.SetLogPath(newPath);
        easyLog.Write(DateTime.Now, "Event2", new Dictionary<string, object> { { "data", "new" } });

        // Assert
        Assert.True(File.Exists(initialPath));
        Assert.True(File.Exists(newPath));

        var newFileContent = File.ReadAllText(newPath);
        Assert.Contains("Event2", newFileContent);
        Assert.DoesNotContain("Event1", newFileContent);

        // Cleanup
        CleanupTestFile(initialPath);
        CleanupTestFile(newPath);
    }

    #endregion

    #region GetCurrentLogPath Tests

    [Fact]
    public void GetCurrentLogPath_ReturnsCurrentPath()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var logPath = GetTempLogPath();

        var easyLog = new EasyLog(formatter.Object, logPath);

        // Act
        var result = easyLog.GetCurrentLogPath();

        // Assert
        Assert.Equal(logPath, result);

        // Cleanup
        CleanupTestFile(logPath);
    }

    [Fact]
    public void GetCurrentLogPath_AfterSetLogPath_ReturnsUpdatedPath()
    {
        // Arrange
        var formatter = new Mock<ILogFormatter>();
        var initialPath = GetTempLogPath();
        var newPath = GetTempLogPath();

        var easyLog = new EasyLog(formatter.Object, initialPath);
        easyLog.SetLogPath(newPath);

        // Act
        var result = easyLog.GetCurrentLogPath();

        // Assert
        Assert.Equal(newPath, result);

        // Cleanup
        CleanupTestFile(initialPath);
        CleanupTestFile(newPath);
    }

    #endregion
}
