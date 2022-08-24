using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Represents a base view model class supporting commands and property chnage notifications.
    /// </summary>
    public abstract class ModelViewBase : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Allows to keep bindings to common application commands.
        /// </summary>
        public CommandBindingCollection CommandBindings { get; } = new();

        protected void AddCommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute = null)
        {
            var commandBinding = canExecute != null ?
                new CommandBinding(command, executed, canExecute)
                : new CommandBinding(command, executed);

            CommandManager.RegisterClassCommandBinding(GetType(), commandBinding);
            CommandBindings.Add(commandBinding);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }
    }
}
