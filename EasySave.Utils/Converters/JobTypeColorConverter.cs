using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

public class JobTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is string jobType)
        {
            return jobType.ToLower() switch
            {
                "full" => new SolidColorBrush(Color.Parse("#3B82F6")),
                "differential" => new SolidColorBrush(Color.Parse("#8B5CF6")),
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
