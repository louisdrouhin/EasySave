using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Converts two values (remaining, total) to size progress text
// Displays processed data vs total with units (e.g., "12.50 MB / 50 MB")
public class SizeProgressConverter : IMultiValueConverter
{
    // Converts two values to size progress text
    // @param values - list containing [remaining, total] in bytes
    // @param targetType - target type (string)
    // @param parameter - optional parameter
    // @param culture - conversion culture
    // @returns formatted string (e.g., "12.50 MB / 50 MB" or "- / -" if no data)
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            long remaining = System.Convert.ToInt64(values[0] ?? 0);
            long total = System.Convert.ToInt64(values[1] ?? 0);

            if (total == 0) return "- / -";

            long processed = total - remaining;
            return $"{FormatSize(processed)} / {FormatSize(total)}";
        }

        return "- / -";
    }

    // Formats a size in bytes to a readable string with unit
    // @param size - size in bytes
    // @returns formatted string (e.g., "12.50 MB")
    private string FormatSize(long size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double doubleSize = size;

        while (doubleSize >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            doubleSize /= 1024;
            suffixIndex++;
        }

        return $"{doubleSize:0.##} {suffixes[suffixIndex]}";
    }
}