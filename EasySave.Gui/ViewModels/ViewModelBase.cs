using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySave.Gui.ViewModels;

// Classe de base pour tous les ViewModels
// Implémente INotifyPropertyChanged pour les bindings XAML
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Met à jour une propriété et déclenche PropertyChanged si la valeur change
    // @param field - référence au champ backing field
    // @param value - nouvelle valeur
    // @param propertyName - nom de la propriété (auto-rempli via CallerMemberName)
    protected void SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    // Déclenche PropertyChanged pour une propriété spécifique
    // @param propertyName - nom de la propriété qui a changé (auto-rempli via CallerMemberName)
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
