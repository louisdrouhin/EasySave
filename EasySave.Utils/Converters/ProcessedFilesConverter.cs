using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Converts two values (remaining, total) to progress text
// Displays number of processed files vs total (e.g., "5 / 10")
public class ProcessedFilesConverter : IMultiValueConverter
{
    // Converts two values to file progress text
    // @param values - list containing [remaining, total] files
    // @param targetType - target type (string)
    // @param parameter - optional parameter
    // @param culture - conversion culture
    // @returns formatted string (e.g., "5 / 10" or "- / -" if no data)
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            long remaining = System.Convert.ToInt64(values[0] ?? 0);
            long total = System.Convert.ToInt64(values[1] ?? 0);

            if (total == 0) return "- / -";

            long processed = total - remaining;
            return $"{processed:N0} / {total:N0}";
        }

        return "- / -";
    }
}
