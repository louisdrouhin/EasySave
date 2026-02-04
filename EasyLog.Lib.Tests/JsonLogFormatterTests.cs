namespace EasyLog.Lib.Tests;

public class JsonLogFormatterTests
{
    [Fact]
    public void Format_WithValidInput_ReturnsValidJson()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestLog";
        var content = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("2025-02-04 10:30:45", result);
        Assert.Contains("TestLog", result);
        Assert.Contains("key1", result);
        Assert.Contains("value1", result);
        Assert.Contains("key2", result);
        Assert.Contains("42", result);
    }

    [Fact]
    public void Format_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = DateTime.Now;
        var name = "TestLog";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            formatter.Format(timestamp, name, null!));
    }

    [Fact]
    public void Format_WithEmptyContent_ReturnsJsonWithEmptyContent()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestLog";
        var content = new Dictionary<string, object>();

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("TestLog", result);
        Assert.Contains("2025-02-04 10:30:45", result);
    }

    [Fact]
    public void Format_WithNullName_ReturnsJsonWithNullName()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        string name = null!;
        var content = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("null", result);
    }

    [Fact]
    public void Format_WithComplexObjects_SerializesSuccessfully()
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "ComplexLog";
        var content = new Dictionary<string, object>
        {
            { "string", "test" },
            { "integer", 123 },
            { "double", 45.67 },
            { "boolean", true },
            { "null", null! }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test", result);
        Assert.Contains("123", result);
        Assert.Contains("45.67", result);
    }

    [Theory]
    [InlineData("yyyy-MM-dd HH:mm:ss")]
    public void Format_TimestampFormat_IsCorrect(string expectedFormat)
    {
        // Arrange
        var formatter = new JsonLogFormatter();
        var timestamp = new DateTime(2025, 12, 25, 23, 59, 59);
        var name = "XmasLog";
        var content = new Dictionary<string, object> { { "test", "value" } };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        string expectedTimestamp = timestamp.ToString(expectedFormat);
        Assert.Contains(expectedTimestamp, result);
    }
}
