using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SImulator.ViewModel.Model;

public sealed class SpecialsAliases : INotifyPropertyChanged
{
    private string? _stakeQuestionAlias = null;

    public string? StakeQuestionAlias { get => _stakeQuestionAlias; set { _stakeQuestionAlias = value; OnPropertyChanged(); } }

    private string? _secretQuestionAlias = null;

    public string? SecretQuestionAlias { get => _secretQuestionAlias; set { _secretQuestionAlias = value; OnPropertyChanged(); } }

    private string? _noRiskQuestionAlias = null;

    public string? NoRiskQuestionAlias { get => _noRiskQuestionAlias; set { _noRiskQuestionAlias = value; OnPropertyChanged(); } }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
