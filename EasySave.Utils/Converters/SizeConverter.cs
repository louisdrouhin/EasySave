using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Converts a size in bytes to a readable string (KB, MB, GB, etc.)
public class SizeConverter : IValueConverter
{
    // Converts a size in bytes to readable format
    // Progressively divides by 1024 to get the appropriate unit
    // @param value - size in bytes (long)
    // @param targetType - target type (string)
    // @param parameter - optional parameter
    // @param culture - conversion culture
    // @returns formatted string with unit (e.g., "12.50 MB")
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is long size)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double doubleSize = size;

            // Divide by 1024 until getting an appropriate value
            while (doubleSize >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                doubleSize /= 1024;
                suffixIndex++;
            }

            return $"{doubleSize:0.##} {suffixes[suffixIndex]}";
        }
        return "0 B";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
