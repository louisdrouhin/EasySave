using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils.Converters;

public class ProcessedFilesConverter : IMultiValueConverter
{
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
