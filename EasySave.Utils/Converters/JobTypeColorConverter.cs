using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Converts a job type to a color for display
// Blue=Full, Purple=Differential
public class JobTypeColorConverter : IValueConverter
{
    // Converts a job type to a color
    // @param value - job type (string: "full" or "differential")
    // @param targetType - target type (IBrush)
    // @param parameter - optional parameter
    // @param culture - conversion culture
    // @returns color associated with the job type
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is string jobType)
        {
            return jobType.ToLower() switch
            {
                "full" => new SolidColorBrush(Color.Parse("#3B82F6")), // Blue
                "differential" => new SolidColorBrush(Color.Parse("#8B5CF6")), // Purple
                _ => new SolidColorBrush(Color.Parse("#6B7280")) // Gray
            };
        }

        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
