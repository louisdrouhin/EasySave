using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Convertit deux valeurs (remaining, total) en texte de progression en tailles
// Affiche les données traitées vs total avec unités (ex: "12.50 MB / 50 MB")
public class SizeProgressConverter : IMultiValueConverter
{
    // Convertit deux valeurs en texte de progression des tailles
    // @param values - liste contenant [remaining, total] en bytes
    // @param targetType - type cible (string)
    // @param parameter - paramètre optionnel
    // @param culture - culture de conversion
    // @returns chaîne formatée (ex: "12.50 MB / 50 MB" ou "- / -" si aucune données)
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            long remaining = System.Convert.ToInt64(values[0] ?? 0);
            long total = System.Convert.ToInt64(values[1] ?? 0);

            if (total == 0) return "- / -";

            long processed = total - remaining;
            return $"{FormatSize(processed)} / {FormatSize(total)}";
        }

        return "- / -";
    }

    // Formate une taille en bytes en chaîne lisible avec unité
    // @param size - taille en bytes
    // @returns chaîne formatée (ex: "12.50 MB")
    private string FormatSize(long size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double doubleSize = size;

        while (doubleSize >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            doubleSize /= 1024;
            suffixIndex++;
        }

        return $"{doubleSize:0.##} {suffixes[suffixIndex]}";
    }
}