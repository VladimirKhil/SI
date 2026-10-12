using Avalonia.Controls;
using Avalonia.Data.Converters;
using SImulator.Properties;
using SImulator.ViewModel.Model;

namespace SImulator.Views;

/// <summary>
/// Players list with scores and score buttons.
/// </summary>
public partial class PlayersTable : UserControl
{
    public static readonly IValueConverter DecisionHint = new FuncValueConverter<DecisionMode, string>(mode => mode switch
    {
        DecisionMode.StarterChoosing => Properties.Resources.ChoosePlayerToStart,
        DecisionMode.SelectDeleter => Properties.Resources.ChoosePlayerToDelete,
        DecisionMode.AnswererChoosing => Properties.Resources.ChoosePlayerToAnswer,
        _ => "",
    });

    public PlayersTable() => InitializeComponent();
}
