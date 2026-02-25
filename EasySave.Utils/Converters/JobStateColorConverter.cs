using Avalonia.Data.Converters;
using Avalonia.Media;
using EasySave.Models;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Convertit un état de job en couleur pour l'affichage
// Vert=Active, Gris=Inactive/Paused
public class JobStateColorConverter : IValueConverter
{
    // Convertit un JobState en couleur
    // @param value - JobState à convertir
    // @param targetType - type cible (IBrush)
    // @param parameter - paramètre optionnel
    // @param culture - culture de conversion
    // @returns couleur associée à l'état du job
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
