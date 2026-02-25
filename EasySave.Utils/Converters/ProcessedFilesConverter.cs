using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Utils.Converters;

// Convertit deux valeurs (remaining, total) en texte de progression
// Affiche le nombre de fichiers traités vs total (ex: "5 / 10")
public class ProcessedFilesConverter : IMultiValueConverter
{
    // Convertit deux valeurs en texte de progression des fichiers
    // @param values - liste contenant [remaining, total] fichiers
    // @param targetType - type cible (string)
    // @param parameter - paramètre optionnel
    // @param culture - culture de conversion
    // @returns chaîne formatée (ex: "5 / 10" ou "- / -" si aucune données)
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
