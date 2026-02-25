namespace EasyLog.Lib.Tests;

public class XmlLogFormatterTests
{
    [Fact]
    public void Format_WithValidInput_ReturnsValidXml()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
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
        Assert.Contains("<logEntry>", result);
        Assert.Contains("</logEntry>", result);
        Assert.Contains("<timestamp>", result);
        Assert.Contains("<name>", result);
        Assert.Contains("<content>", result);
        Assert.Contains("<key1>value1</key1>", result);
        Assert.Contains("<key2>42</key2>", result);
        Assert.Contains("<key3>True</key3>", result);
    }

    [Fact]
    public void Format_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = DateTime.Now;
        var name = "TestLog";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            formatter.Format(timestamp, name, null!));
    }

    [Fact]
    public void Format_WithEmptyContent_ReturnsXmlWithEmptyContent()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "TestLog";
        var content = new Dictionary<string, object>();

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("TestLog", result);
        Assert.Contains("2025-02-04 10:30:45", result);
        Assert.Matches("<content\\s*/>", result); // Accepts <content /> or <content/>
    }

    [Fact]
    public void Format_WithNullName_ReturnsXmlWithEmptyName()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        string name = null!;
        var content = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Matches("<name\\s*/>", result); // Accepts <name /> or <name/>
    }

    [Fact]
    public void Format_WithComplexObjects_SerializesSuccessfully()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "ComplexLog";
        var content = new Dictionary<string, object>
        {
            { "string", "test" },
            { "integer", 123 },
            { "double", 45.67 },
            { "boolean", true },
            { "nullValue", null! }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("<string>test</string>", result);
        Assert.Contains("<integer>123</integer>", result);
        Assert.Contains("<double>", result); // Verifies that the double element exists
        Assert.Contains("</double>", result);
        Assert.Contains("<boolean>True</boolean>", result);
        Assert.Contains("<nullValue>", result); // Verifies that the nullValue element exists
    }

    [Theory]
    [InlineData("yyyy-MM-dd HH:mm:ss")]
    public void Format_TimestampFormat_IsCorrect(string expectedFormat)
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 12, 25, 23, 59, 59);
        var name = "XmasLog";
        var content = new Dictionary<string, object> { { "test", "value" } };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        string expectedTimestamp = timestamp.ToString(expectedFormat);
        Assert.Contains(expectedTimestamp, result);
    }

    [Fact]
    public void Format_WithInvalidXmlCharactersInKeys_SanitizesKeys()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "SanitizeTest";
        var content = new Dictionary<string, object>
        {
            { "invalid key", "value1" },
            { "key@special", "value2" },
            { "123numeric", "value3" }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("<invalid_key>value1</invalid_key>", result);
        Assert.Contains("<key_special>value2</key_special>", result);
        Assert.Contains("<_123numeric>value3</_123numeric>", result);
    }

    [Fact]
    public void Format_WithSpecialXmlCharacters_EscapesCorrectly()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "EscapeTest";
        var content = new Dictionary<string, object>
        {
            { "data", "<>&\"'" }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        // The XmlWriter automatically escapes special characters
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
    }

    [Fact]
    public void Close_WithExistingFile_AddsClosingTag()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<logs><logEntry></logEntry>");

        try
        {
            // Act
            formatter.Close(tempFile);

            // Assert
            var content = File.ReadAllText(tempFile);
            Assert.EndsWith("</logs>", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Close_WithAlreadyClosedFile_DoesNotAddExtraTag()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<logs></logs>");

        try
        {
            // Act
            formatter.Close(tempFile);

            // Assert
            var content = File.ReadAllText(tempFile);
            // Should not have double closing
            Assert.Equal(1, content.Split("</logs>").Length - 1);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Close_WithNonExistentFile_DoesNotThrow()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");

        // Act & Assert - should not throw an exception
        var exception = Record.Exception(() => formatter.Close(nonExistentFile));
        Assert.Null(exception);
    }

    [Fact]
    public void Format_WithPathValues_ContainsPaths()
    {
        // Arrange
        var formatter = new XmlLogFormatter();
        var timestamp = new DateTime(2025, 2, 4, 10, 30, 45);
        var name = "PathTest";
        var content = new Dictionary<string, object>
        {
            { "sourcePath", @"C:\Source\Files" },
            { "targetPath", @"D:\Backup\Files" }
        };

        // Act
        var result = formatter.Format(timestamp, name, content);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("sourcePath", result);
        Assert.Contains("targetPath", result);
    }
}
