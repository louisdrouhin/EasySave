using Avalonia.Data.Converters;
using Avalonia.Media;
using EasySave.Models;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

public class JobStateColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is JobState state)
        {
            return state switch
            {
                JobState.Active => new SolidColorBrush(Color.Parse("#22C55E")), // Green
                JobState.Inactive => new SolidColorBrush(Color.Parse("#6B7280")), // Gray
                _ => new SolidColorBrush(Color.Parse("#6B7280"))
            };
        }

        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
