using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using System.Text;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class StatisticsViewModel: WorkspaceViewModel
    {
        private readonly QDocument _document = null;

        public override string Header => "Статистика";

        private bool _checkEmptyAuthors = false;

        public bool CheckEmptyAuthors
        {
            get => _checkEmptyAuthors;
            set
            {
                if (_checkEmptyAuthors != value)
                {
                    _checkEmptyAuthors = value; OnPropertyChanged(); Create_Executed();
                }
            }
        }

        private bool _checkEmptySources = false;

        public bool CheckEmptySources
        {
            get => _checkEmptySources;
            set
            {
                if (_checkEmptySources != value)
                {
                    _checkEmptySources = value; OnPropertyChanged(); Create_Executed();
                }
            }
        }

        private bool _checkBrackets = true;

        public bool CheckBrackets
        {
            get { return _checkBrackets; }
            set
            {
                if (_checkBrackets != value)
                { 
                    _checkBrackets = value; OnPropertyChanged(); Create_Executed();
                }
            }
        }

        private string _result;

        public string Result
        {
            get { return _result; }
            set { _result = value; OnPropertyChanged(); }
        }

        public ICommand Create { get; private set; }

        public StatisticsViewModel(QDocument document)
        {
            _document = document;

            Create = new SimpleCommand(Create_Executed);
            Create_Executed();
        }

        private void Create_Executed(object arg = null)
        {
            var stats = new StringBuilder();
            stats.Append(Resources.NameOfPackage);
            stats.Append(' ');
            stats.AppendLine(_document.Document.Package.Name);
            stats.Append(Resources.NumOfRounds);
            stats.Append(':');
            stats.AppendLine(_document.Document.Package.Rounds.Count.ToString());

            int allt = 0, allq = 0;
            _document.Document.Package.Rounds.ForEach(round =>
            {
                allt += round.Themes.Count;
                round.Themes.ForEach(theme => allq += theme.Questions.Count);
            });

            stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfThemes, allt));
            stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfQuests, allq));
            stats.AppendLine();
            foreach (var round in _document.Document.Package.Rounds)
            {
                stats.AppendLine(string.Format("{0}: {1}", Resources.Round, round.Name));
                stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfThemes, round.Themes.Count));

                foreach (var theme in round.Themes)
                {
                    var themeData = new StringBuilder();
                    bool here = false;
                    bool tb1 = theme.Info.Comments.Text.Contains(Resources.Undefined);
                    bool tb2 = theme.Info.Authors.Count == 0 && _checkEmptyAuthors;
                    bool tb3 = !Utils.ValidateTextBrackets(theme.Name) || !Utils.ValidateTextBrackets(theme.Info.Comments.Text);
                    bool tb4 = round.Type == RoundTypes.Standart && theme.Questions.Count < 5;
                    if (tb1 || tb2 || tb3 || tb4)
                    {
                        if (!here)
                        {
                            here = true;
                            themeData.AppendLine(string.Format("{0}: {1}", Resources.Theme, theme.Name));
                        }

                        if (tb1)
                        {
                            themeData.AppendLine(Resources.Unrecognized);
                        }

                        if (tb2)
                        {
                            themeData.AppendLine(Resources.NoAuthors);
                        }

                        if (tb4)
                        {
                            themeData.AppendLine(Resources.FewQuestions);
                        }
                    }

                    foreach (var quest in theme.Questions)
                    {
                        bool b1 = quest.Scenario.ToString() == Resources.Question;
                        bool b2 = quest.Right.Count == 1 && quest.Right[0] == Resources.RightAnswer;
                        bool b3 = quest.Info.Sources.Count == 0 && _checkEmptySources;

                        StringBuilder bracketsData = new StringBuilder();
                        if (_checkBrackets)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                string text = string.Empty;
                                string comment = string.Empty;
                                switch (i)
                                {
                                    case 0:
                                        text = quest.Scenario.ToString();
                                        comment = Resources.QText;
                                        break;

                                    case 1:
                                        if (quest.Right.Count == 0)
                                            continue;

                                        text = quest.Right[0];
                                        comment = Resources.QAnswer;
                                        break;

                                    case 2:
                                        if (quest.Info.Sources.Count == 0)
                                            continue;

                                        text = quest.Info.Sources[0];
                                        comment = Resources.Source;
                                        break;

                                    case 3:
                                        text = quest.Info.Comments.Text;
                                        comment = Resources.Comment;
                                        break;
                                }

                                if (!Utils.ValidateTextBrackets(text))
                                {
                                    bracketsData.Append(quest.Price.ToString());
                                    bracketsData.Append(": ");
                                    bracketsData.Append(Resources.WrongBrackets);
                                    bracketsData.AppendFormat(" ({0})", comment);
                                    bracketsData.Append(": ");
                                    bracketsData.AppendLine(text);
                                }
                            }
                        }

                        bool b4 = bracketsData.Length > 0;

                        if (b1 || b2 || b3 || b4)
                        {
                            if (!here)
                            {
                                here = true;
                                stats.AppendLine(string.Format("{0}: {1}", Resources.Theme, theme.Name));
                            }
                        }

                        if (b1)
                        {
                            themeData.AppendLine(string.Format("{0}: {1}", quest.Price, Resources.NoQuestion));
                        }

                        if (b2)
                        {
                            themeData.AppendLine(string.Format("{0}: {1}", quest.Price, Resources.NoAnswer));
                        }

                        if (b3)
                        {
                            themeData.AppendLine(string.Format("{0}: {1}", quest.Price, Resources.NoSource));
                        }

                        if (b4)
                        {
                            themeData.Append(bracketsData);
                        }
                    }

                    if (themeData.Length > 0)
                    {
                        stats.AppendLine(themeData.ToString());
                    }
                }

                stats.AppendLine();
            }

            stats.AppendLine(_document.CheckLinks(true));

            Result = stats.ToString();
        }
    }
}
