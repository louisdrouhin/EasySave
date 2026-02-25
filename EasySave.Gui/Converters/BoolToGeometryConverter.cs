using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EasySave.Gui.Converters;

// Convertit un booléen en icône chevron
// Affiche une flèche bas si true (expandu), flèche droite si false (collapsé)
public class BoolToGeometryConverter : IValueConverter
{
    // Convertit une valeur booléenne en géométrie d'icône
    // @param value - booléen indiquant si expandu
    // @param targetType - type cible (Geometry)
    // @param parameter - paramètre optionnel
    // @param culture - culture de conversion
    // @returns géométrie du chevron (bas ou droite)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is bool isExpanded)
        {
            // Chevron vers le bas si expandu, flèche droite sinon
            if (isExpanded)
                return Geometry.Parse("M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z"); // ChevronDown
            else
                return Geometry.Parse("M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"); // ChevronRight
        }
        return null;
    }

    // Conversion inverse non implémentée (lecture seule)
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        return null;
    }
}
