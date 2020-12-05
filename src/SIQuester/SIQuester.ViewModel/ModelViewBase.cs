using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Базовый класс для моделей отображения с поддержкой команд и извещений об изменениях
    /// </summary>
    public abstract class ModelViewBase: INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Коллекция, позволяющая осуществить привязку к стандартным командам
        /// </summary>
        public CommandBindingCollection CommandBindings { get; } = new CommandBindingCollection();

        protected void AddCommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute = null)
        {
            var commandBinding = canExecute != null ?
                new CommandBinding(command, executed, canExecute)
                : new CommandBinding(command, executed);

            CommandManager.RegisterClassCommandBinding(GetType(), commandBinding);
            CommandBindings.Add(commandBinding);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Члены IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            
        }

        #endregion
    }
}
