using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Команда, выражаемая пользовательским интерфейсом
/// </summary>
public sealed class UICommand
{
    public string Header { get; set; }
    public ICommand Command { get; set; }
}
