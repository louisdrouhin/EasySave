using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EasySave.Gui.Converters;

// Converts a boolean to a chevron icon
// Displays a down arrow if true (expanded), right arrow if false (collapsed)
public class BoolToGeometryConverter : IValueConverter
{
    // Converts a boolean value to icon geometry
    // @param value - boolean indicating if expanded
    // @param targetType - target type (Geometry)
    // @param parameter - optional parameter
    // @param culture - conversion culture
    // @returns chevron geometry (down or right)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is bool isExpanded)
        {
            // Chevron down if expanded, right arrow otherwise
            if (isExpanded)
                return Geometry.Parse("M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z"); // ChevronDown
            else
                return Geometry.Parse("M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"); // ChevronRight
        }
        return null;
    }

    // Reverse conversion not implemented (read-only)
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        return null;
    }
}
