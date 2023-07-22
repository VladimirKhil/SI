using Microsoft.Extensions.Logging;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Properties;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Allows to select themes to form a new package.
/// </summary>
public sealed class SelectThemesViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;
    private readonly AppOptions _appOptions;

    public override string Header => $"{_document.Document.Package.Name}: {Resources.ThemesSelection}";

    public int Total => _document.Document.Package.Rounds.Sum(round => round.Themes.Count);

    private int _from = 1;

    public int From
    {
        get => _from;
        set
        {
            if (_from != value)
            {
                _from = value;
                OnPropertyChanged();
            }
        }
    }

    private int _to = 1;

    public int To
    {
        get => _to;
        set
        {
            if (_to != value)
            {
                _to = value;
                OnPropertyChanged();
            }
        }
    }

    public IEnumerable<SelectableTheme> Themes { get; }

    public ICommand Select { get; private set; }

    public ICommand Select2 { get; private set; }

    private readonly ILoggerFactory _loggerFactory;

    public SelectThemesViewModel(QDocument document, AppOptions appOptions, ILoggerFactory loggerFactory)
    {
        _document = document;
        _appOptions = appOptions;
        _loggerFactory = loggerFactory;

        Themes = _document.Document.Package.Rounds
            .SelectMany(round => round.Themes)
            .Select(theme => new SelectableTheme(theme))
            .ToArray();

        Select = new SimpleCommand(Select_Executed);
        Select2 = new SimpleCommand(Select2_Executed);
    }

    private async void Select_Executed(object? arg)
    {
        try
        {
            var authors = _document.Document.Package.Info.Authors;
            var newDocument = SIDocument.Create(_document.Document.Package.Name, authors.Count > 0 ? authors[0] : Resources.Empty);

            var mainRound = newDocument.Package.CreateRound(RoundTypes.Standart, Resources.ThemesCollection);

            var allthemes = new List<Theme>();
            _document.Document.Package.Rounds.ForEach(round => round.Themes.ForEach(allthemes.Add));

            for (var index = _from; index <= _to; index++)
            {
                var newTheme = allthemes[index - 1].Clone();
                mainRound.Themes.Add(newTheme);

                // Export neccessary collections
                await _document.Document.CopyCollectionsAsync(newDocument, allthemes[index - 1]);
            }

            if (_appOptions.UpgradePackages)
            {
                newDocument.Upgrade();
            }

            OnNewItem(new QDocument(newDocument, _document.StorageContext, _loggerFactory) { FileName = newDocument.Package.Name });
        }
        catch (Exception exc)
        {
            _document.OnError(exc);
        }
    }

    private async void Select2_Executed(object? arg)
    {
        try
        {
            var authors = _document.Document.Package.Info.Authors;
            var newDocument = SIDocument.Create(_document.Document.Package.Name, authors.Count > 0 ? authors[0] : Resources.Empty);
            var mainRound = newDocument.Package.CreateRound(RoundTypes.Standart, Resources.ThemesCollection);

            var allthemes = Themes.Where(st => st.IsSelected).Select(st => st.Theme);

            foreach (var theme in allthemes)
            {
                var newTheme = theme.Clone();
                mainRound.Themes.Add(newTheme);

                // Export neccessary collections
                await _document.Document.CopyCollectionsAsync(newDocument, theme);
            }

            if (_appOptions.UpgradePackages)
            {
                newDocument.Upgrade();
            }

            OnNewItem(new QDocument(newDocument, _document.StorageContext, _loggerFactory) { FileName = newDocument.Package.Name });
        }
        catch (Exception exc)
        {
            _document.OnError(exc);
        }
    }
}
