using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Workspaces.Sidebar;
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

    /// <summary>
    /// Statistics result.
    /// </summary>
    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(); }
    }

    private WarningViewModel[] _warnings = Array.Empty<WarningViewModel>();

    /// <summary>
    /// Statistics warnings.
    /// </summary>
    public WarningViewModel[] Warnings
    {
        get => _warnings;
        set
        {
            if (_warnings != value)
            {
                _warnings = value;
                OnPropertyChanged();
            }
        }
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
        var warnings = new List<WarningViewModel>();

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

        var checkResult = _document.CheckLinks(true);

        warnings.AddRange(checkResult.Item1);
        stats.Append(checkResult.Item2);

        Result = stats.ToString();
        Warnings = warnings.ToArray();
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

                foreach (var question in theme.Questions)
                {
                    var questionText = question.GetText();

                    var emptyNormal = question.Price == Question.InvalidPrice || question.TypeName == QuestionTypes.SecretNoQuestion;
                    
                    var emptyQuestion = !emptyNormal
                        && (questionText == "" || questionText == Resources.Question)
                        && !question.HasMediaContent();

                    var noAnswer = !emptyNormal && (question.Right.Count == 0 || question.Right.Count == 1 && string.IsNullOrWhiteSpace(question.Right[0]));
                    var emptySources = question.Info.Sources.Count == 0 && _checkEmptySources;

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
                                    text = question.GetText();
                                    comment = Resources.QText;
                                    break;

                                case 1:
                                    if (question.Right.Count == 0)
                                        continue;

                                    text = question.Right[0];
                                    comment = Resources.QAnswer;
                                    break;

                                case 2:
                                    if (question.Info.Sources.Count == 0)
                                        continue;

                                    text = question.Info.Sources[0];
                                    comment = Resources.Source;
                                    break;

                                case 3:
                                    text = question.Info.Comments.Text;
                                    comment = Resources.Comment;
                                    break;
                            }

                            if (!Utils.ValidateTextBrackets(text))
                            {
                                bracketsData.Append(question.Price);
                                bracketsData.Append(": ");
                                bracketsData.Append(Resources.WrongBrackets);
                                bracketsData.AppendFormat(" ({0})", comment);
                                bracketsData.Append(": ");
                                bracketsData.AppendLine(text);
                            }
                        }
                    }

                    var hasBracketsIssues = bracketsData.Length > 0;

                    if (emptyQuestion || noAnswer || emptySources || hasBracketsIssues)
                    {
                        if (!here)
                        {
                            here = true;
                            stats.AppendLine(string.Format("{0}: {1}", Resources.Theme, theme.Name));
                        }
                    }

                    if (emptyQuestion)
                    {
                        themeData.AppendLine(string.Format("{0}: {1}", question.Price, Resources.NoQuestion));
                    }

                    if (noAnswer)
                    {
                        themeData.AppendLine(string.Format("{0}: {1}", question.Price, Resources.NoAnswer));
                    }

                    if (emptySources)
                    {
                        themeData.AppendLine(string.Format("{0}: {1}", question.Price, Resources.NoSource));
                    }

                    if (hasBracketsIssues)
                    {
                        themeData.Append(bracketsData);
                    }

                    foreach (var content in question.GetContent())
                    {
                        if (content.Type == ContentTypes.Audio && content.Placement != ContentPlacements.Background)
                        {
                            themeData.AppendLine($"{question.Price}: {string.Format(Resources.AudioIsNotOnBackground, content.Value)}");
                        }
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
