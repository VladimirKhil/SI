using SIPackages;

namespace SIQuester.ViewModel;

public sealed class SelectableTheme
{
    public bool IsSelected { get; set; }

    public string Name => Theme.Name;

    public Theme Theme { get; }

    public SelectableTheme(Theme theme) => Theme = theme;
}
