using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySave.Gui.ViewModels;

// Base class for all ViewModels
// Implements INotifyPropertyChanged for XAML bindings
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Updates a property and raises PropertyChanged if the value changes
    // @param field - reference to the backing field
    // @param value - new value
    // @param propertyName - property name (auto-filled via CallerMemberName)
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

    // Raises PropertyChanged for a specific property
    // @param propertyName - property name that changed (auto-filled via CallerMemberName)
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
