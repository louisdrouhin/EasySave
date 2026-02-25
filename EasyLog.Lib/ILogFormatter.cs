namespace EasyLog.Lib;

// Interface for log formatters
// Implemented by JsonLogFormatter and XmlLogFormatter
public interface ILogFormatter
{
    // Formats a log entry
    // @param timestamp - date/time of the entry
    // @param name - name of the backup
    // @param content - entry content (dictionary of properties)
    // @returns formatted string (JSON or XML)
    string Format(DateTime timestamp, string name, Dictionary<string, object> content);

    // Closes the log file (adds end markers if necessary)
    // @param filePath - path to the file to close
    void Close(string filePath) { }
}
