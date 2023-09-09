using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using System.Text;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class StatisticsViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;

    public override string Header => Resources.Statistic;

    private bool _checkEmptyAuthors = false;

    public bool CheckEmptyAuthors
    {
        get => _checkEmptyAuthors;
        set
        {
            if (_checkEmptyAuthors != value)
            {
                _checkEmptyAuthors = value;
                OnPropertyChanged();
                Create_Executed();
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
                _checkEmptySources = value;
                OnPropertyChanged();
                Create_Executed();
            }
        }
    }

    private bool _checkBrackets = true;

    public bool CheckBrackets
    {
        get => _checkBrackets;
        set
        {
            if (_checkBrackets != value)
            {
                _checkBrackets = value;
                OnPropertyChanged();
                Create_Executed();
            }
        }
    }

    private string _result = "";

    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(); }
    }

    public ICommand Create { get; private set; }

    public ICommand RemoveUnusedFiles { get; private set; }

    public StatisticsViewModel(QDocument document)
    {
        _document = document;

        Create = new SimpleCommand(Create_Executed);
        RemoveUnusedFiles = new SimpleCommand(RemoveUnusedFiles_Executed);
        Create_Executed();
    }

    private void Create_Executed(object? arg = null)
    {
        var stats = new StringBuilder();
        stats.Append(Resources.NumOfRounds);
        stats.Append(": ");
        stats.AppendLine(_document.Document.Package.Rounds.Count.ToString());

        int themeCount = 0, questionCount = 0;

        _document.Document.Package.Rounds.ForEach(round =>
        {
            themeCount += round.Themes.Count;
            round.Themes.ForEach(theme => questionCount += theme.Questions.Count);
        });

        stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfThemes, themeCount));
        stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfQuests, questionCount));
        stats.AppendLine();

        CheckText(stats);

        stats.AppendLine(_document.CheckLinks(true));

        Result = stats.ToString();
    }

    private void CheckText(StringBuilder stats)
    {
        foreach (var round in _document.Document.Package.Rounds)
        {
            stats.AppendLine(string.Format("{0}: {1}", Resources.Round, round.Name));
            stats.AppendLine(string.Format("{0}: {1}", Resources.NumOfThemes, round.Themes.Count));

            foreach (var theme in round.Themes)
            {
                var themeData = new StringBuilder();
                bool here = false;

                var unrecognizedText = theme.Info.Comments.Text.Contains(Resources.Undefined);
                var noAuthors = theme.Info.Authors.Count == 0 && _checkEmptyAuthors;
                var invalidBrackets = !Utils.ValidateTextBrackets(theme.Name) || !Utils.ValidateTextBrackets(theme.Info.Comments.Text);
                var missingQuestions = round.Type == RoundTypes.Standart && theme.Questions.Count < 5;

                if (unrecognizedText || noAuthors || invalidBrackets || missingQuestions)
                {
                    if (!here)
                    {
                        here = true;
                        themeData.AppendLine(string.Format("{0}: {1}", Resources.Theme, theme.Name));
                    }

                    if (unrecognizedText)
                    {
                        themeData.AppendLine(Resources.Unrecognized);
                    }

                    if (noAuthors)
                    {
                        themeData.AppendLine(Resources.NoAuthors);
                    }

                    if (missingQuestions)
                    {
                        themeData.AppendLine(Resources.FewQuestions);
                    }
                }

                foreach (var quest in theme.Questions)
                {
                    var questionText = quest.GetText();

                    var emptyQuestion = quest.Price > Question.InvalidPrice
                        && quest.TypeName != QuestionTypes.SecretNoQuestion
                        && (questionText == "" || questionText == Resources.Question)
                        && !quest.HasMediaContent();

                    var noAnswer = quest.Right.Count == 1 && quest.Right[0] == Resources.RightAnswer;
                    var emptySources = quest.Info.Sources.Count == 0 && _checkEmptySources;

                    var bracketsData = new StringBuilder();

                    if (_checkBrackets)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            var text = string.Empty;
                            var comment = string.Empty;

                            switch (i)
                            {
                                case 0:
                                    text = quest.GetText();
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
                                bracketsData.Append(quest.Price);
                                bracketsData.Append(": ");
                                bracketsData.Append(Resources.WrongBrackets);
                                bracketsData.AppendFormat(" ({0})", comment);
                                bracketsData.Append(": ");
                                bracketsData.AppendLine(text);
                            }
                        }
                    }

                    bool b4 = bracketsData.Length > 0;

                    if (emptyQuestion || noAnswer || emptySources || b4)
                    {
                        if (!here)
                        {
                            here = true;
                            stats.AppendLine(string.Format("{0}: {1}", Resources.Theme, theme.Name));
                        }
                    }

                    if (emptyQuestion)
                    {
                        themeData.AppendLine(string.Format("{0}: {1}", quest.Price, Resources.NoQuestion));
                    }

                    if (noAnswer)
                    {
                        themeData.AppendLine(string.Format("{0}: {1}", quest.Price, Resources.NoAnswer));
                    }

                    if (emptySources)
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
    }

    private void RemoveUnusedFiles_Executed(object? arg) => _document.RemoveUnusedFiles();
}
