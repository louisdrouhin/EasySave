using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Convertit une taille en bytes en chaîne lisible (KB, MB, GB, etc.)
public class SizeConverter : IValueConverter
{
    // Convertit une taille en bytes en format lisible
    // Divise progressivement par 1024 pour obtenir l'unité appropriée
    // @param value - taille en bytes (long)
    // @param targetType - type cible (string)
    // @param parameter - paramètre optionnel
    // @param culture - culture de conversion
    // @returns chaîne formatée avec unité (ex: "12.50 MB")
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is long size)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double doubleSize = size;

            // Divise par 1024 jusqu'à obtenir une valeur appropriée
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
