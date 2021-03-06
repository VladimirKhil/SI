using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Выбор тем для выгрузки
    /// </summary>
    public sealed class SelectThemesViewModel: WorkspaceViewModel
    {
        private readonly QDocument _document = null;

        public override string Header => string.Format("{0}: выбор тем", _document.Document.Package.Name);

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

        public SelectThemesViewModel(QDocument document)
        {
            _document = document;
            Themes = _document.Document.Package.Rounds
                .SelectMany(round => round.Themes)
                .Select(theme => new SelectableTheme(theme))
                .ToArray();

            Select = new SimpleCommand(Select_Executed);
            Select2 = new SimpleCommand(Select2_Executed);
        }

        private async void Select_Executed(object arg)
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

                    // Выгрузим с собой необходимые коллекции
                    await _document.Document.CopyCollections(newDocument, allthemes[index - 1]);
                }

                OnNewItem(new QDocument(newDocument, _document.StorageContext) { FileName = newDocument.Package.Name });
            }
            catch (Exception exc)
            {
                _document.OnError(exc);
            }
        }

        private async void Select2_Executed(object arg)
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

                    // Выгрузим с собой необходимые коллекции
                    await _document.Document.CopyCollections(newDocument, theme);
                }

                OnNewItem(new QDocument(newDocument, _document.StorageContext) { FileName = newDocument.Package.Name });
            }
            catch (Exception exc)
            {
                _document.OnError(exc);
            }
        }
    }
}
