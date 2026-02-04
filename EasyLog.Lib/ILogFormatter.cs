namespace EasyLog.Lib;

public interface ILogFormatter
{
    string Format(DateTime timestamp, string name, Dictionary<string, object> content);
}
