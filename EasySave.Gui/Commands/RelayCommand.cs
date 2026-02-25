using System;
using System.Windows.Input;

namespace EasySave.Gui.Commands;

// Implémentation simple d'une commande ICommand pour les bindings MVVM
// Encapsule une action et une condition d'exécution optionnelle
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    // Crée une RelayCommand
    // @param execute - action à exécuter quand la commande est appelée
    // @param canExecute - prédicat optionnel pour déterminer si la commande peut s'exécuter
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    // Vérifie si la commande peut s'exécuter
    // @param parameter - paramètre passé à la commande
    // @returns true si la commande peut s'exécuter
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    // Exécute la commande
    // @param parameter - paramètre passé à la commande
    public void Execute(object? parameter) => _execute(parameter);

    // Déclenche l'événement CanExecuteChanged pour actualiser l'état des boutons
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
