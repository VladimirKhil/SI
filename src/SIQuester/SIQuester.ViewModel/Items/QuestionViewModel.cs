using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using System.Collections.Specialized;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a package question view model.
/// </summary>
public sealed class QuestionViewModel : ItemViewModel<Question>
{
    public ThemeViewModel? OwnerTheme { get; set; }

    public override IItemViewModel? Owner => OwnerTheme;

    public AnswersViewModel Right { get; private set; }

    public AnswersViewModel Wrong { get; private set; }

    public ScenarioViewModel Scenario { get; private set; }

    public QuestionTypeViewModel Type { get; private set; }

    public event Action<QuestionViewModel, string> TypeNameChanged;

    public string TypeName
    {
        get => Model.TypeName;
        set
        {
            if (Model.TypeName == value)
            {
                return;
            }

            var oldValue = Model.TypeName;
            Model.TypeName = value;
            TypeNameChanged?.Invoke(this, oldValue);
            OnPropertyChanged();
        }
    }

    public StepParametersViewModel? Parameters { get; private set; }

    public ICommand AddComplexAnswer { get; private set; }

    public ICommand RemoveComplexAnswer { get; private set; }

    public SimpleCommand AddWrongAnswers { get; private set; }

    public ICommand ClearType { get; private set; }

    /// <summary>
    /// Upgraded package flag.
    /// </summary>
    public bool IsUpgraded => OwnerTheme?.OwnerRound?.OwnerPackage?.IsUpgraded == true;

    public override ICommand Add
    {
        get => null;
        protected set { }
    }

    public override ICommand Remove { get; protected set; }

    public ICommand Clone { get; private set; }

    public ICommand SetQuestionType { get; private set; }

    public ICommand SetAnswerType { get; private set; }

    public ICommand SwitchEmpty { get; private set; }

    /// <summary>
    /// Question answer type.
    /// </summary>
    public string AnswerType
    {
        get
        {
            if (Parameters == null)
            {
                return StepParameterValues.SetAnswerTypeType_Text;
            }
            
            if (!Parameters.TryGetValue(QuestionParameterNames.AnswerType, out var answerTypeParameter))
            {
                return StepParameterValues.SetAnswerTypeType_Text;
            }

            return answerTypeParameter.Model.SimpleValue;
        }
    }

    public QuestionViewModel(Question question)
        : base(question)
    {
        Type = new QuestionTypeViewModel(question.Type);

        Right = new AnswersViewModel(this, question.Right, true);
        Wrong = new AnswersViewModel(this, question.Wrong, false);
        Scenario = new ScenarioViewModel(this, question.Scenario);

        if (question.Parameters != null)
        {
            Parameters = new StepParametersViewModel(this, question.Parameters);
        }

        BindHelper.Bind(Right, question.Right);
        BindHelper.Bind(Wrong, question.Wrong);

        AddComplexAnswer = new SimpleCommand(AddComplexAnswer_Executed);
        RemoveComplexAnswer = new SimpleCommand(RemoveComplexAnswer_Executed);
        AddWrongAnswers = new SimpleCommand(AddWrongAnswers_Executed);

        ClearType = new SimpleCommand(ClearType_Executed);

        Clone = new SimpleCommand(CloneQuestion_Executed);
        Remove = new SimpleCommand(RemoveQuestion_Executed);

        SetQuestionType = new SimpleCommand(SetQuestionType_Executed);
        SetAnswerType = new SimpleCommand(SetAnswerType_Executed);
        SwitchEmpty = new SimpleCommand(SwitchEmpty_Executed);

        Wrong.CollectionChanged += Wrong_CollectionChanged;
    }

    private void ClearType_Executed(object? arg) => TypeName = QuestionTypes.Default;

    private void Wrong_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        AddWrongAnswers.CanBeExecuted = Model.Wrong.Count == 0;

    private void AddComplexAnswer_Executed(object? arg)
    {
        if (IsUpgraded)
        {
            Parameters?.AddAnswer(new StepParameterViewModel(this, new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>(new[]
                {
                    new ContentItem { Type = AtomTypes.Text, Placement = ContentPlacements.Screen, Value = "" }
                })
            }));

            return;
        }

        if (Scenario.IsComplex)
        {
            return;
        }

        var document = OwnerTheme.OwnerRound.OwnerPackage.Document;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            Scenario.AddMarker_Executed(arg);
            Scenario.AddText_Executed(arg);

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void RemoveComplexAnswer_Executed(object? arg)
    {
        if (IsUpgraded)
        {
            Parameters?.RemoveAnswer();
            return;
        }

        if (!Scenario.IsComplex)
        {
            return;
        }

        var document = OwnerTheme.OwnerRound.OwnerPackage.Document;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            for (var i = 0; i < Scenario.Count; i++)
            {
                if (Scenario[i].Model.Type == AtomTypes.Marker)
                {
                    while (i < Scenario.Count)
                    {
                        Scenario.RemoveAt(i);
                    }

                    break;
                }
            }

            if (Scenario.Count == 0)
            {
                Scenario.AddText_Executed(null);
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void AddWrongAnswers_Executed(object? arg)
    {
        QDocument.ActivatedObject = Wrong;
        Wrong.Add("");
    }

    private void CloneQuestion_Executed(object? arg)
    {
        if (OwnerTheme == null)
        {
            return;
        }

        var quest = Model.Clone();
        var newQuestionViewModel = new QuestionViewModel(quest);
        OwnerTheme.Questions.Add(newQuestionViewModel);
        OwnerTheme.OwnerRound.OwnerPackage.Document.Navigate.Execute(newQuestionViewModel);
    }

    private void RemoveQuestion_Executed(object? arg) => OwnerTheme?.Questions.Remove(this);

    private void SetQuestionType_Executed(object? arg)
    {
        if (IsUpgraded)
        {
            var typeName = (string?)arg ?? "";
            TypeName = typeName == QuestionTypes.Default ? "?" : typeName;
            return;
        }

        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        Type.Model.Name = (string)arg;
    }

    private void SetAnswerType_Executed(object? arg)
    {
        if (Parameters == null || arg == null)
        {
            return;
        }

        var answerType = (string)arg;

        if (answerType == StepParameterValues.SetAnswerTypeType_Text)
        {
            // Default value; remove parameter
            Parameters.RemoveParameter(QuestionParameterNames.AnswerType);
            Parameters.RemoveParameter(QuestionParameterNames.AnswerOptions);
            OnPropertyChanged(nameof(AnswerType));
            return;
        }

        if (!Parameters.TryGetValue(QuestionParameterNames.AnswerType, out var answerTypeParameter))
        {
            answerTypeParameter = new StepParameterViewModel(this, new StepParameter
            {
                Type = StepParameterTypes.Simple,
                SimpleValue = answerType
            });

            Parameters.AddParameter(QuestionParameterNames.AnswerType, answerTypeParameter);
        }
        else
        {
            answerTypeParameter.Model.SimpleValue = answerType;
        }

        if (answerType == StepParameterValues.SetAnswerTypeType_Select)
        {
            var options = new StepParameter
            {
                Type = StepParameterTypes.Group,
                GroupValue = new StepParameters()
            };

            static StepParameter answerOptionGenerator() => new()
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>
                {
                    new ContentItem { Type = AtomTypes.Text, Value = "" },
                }
            };

            for (var i = 0; i < AppSettings.Default.SelectOptionCount; i++)
            {
                var label = i < 26 ? ((char)('A' + i)).ToString() : 'A' + (i - 25).ToString();
                options.GroupValue.Add(label, answerOptionGenerator());
            }

            var optionsViewModel = new StepParameterViewModel(this, options);

            Parameters.AddParameter(QuestionParameterNames.AnswerOptions, optionsViewModel);
        }

        OnPropertyChanged(nameof(AnswerType));
    }

    private void SwitchEmpty_Executed(object? arg)
    {
        var document = OwnerTheme.OwnerRound.OwnerPackage.Document;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            if (Model.Price == Question.InvalidPrice)
            {
                Model.Price = AppSettings.Default.QuestionBase * ((OwnerTheme?.Questions.IndexOf(this) ?? 0) + 1);

                if (IsUpgraded)
                {
                    Model.Parameters = new StepParameters
                    {
                        [QuestionParameterNames.Question] = new StepParameter
                        {
                            Type = StepParameterTypes.Content,
                            ContentValue = new List<ContentItem>
                            {
                                new ContentItem { Type = AtomTypes.Text, Value = "" },
                            }
                        }
                    };
                }
                else
                {
                    Model.Scenario.Clear();
                    Model.Scenario.Add(new Atom());
                    Model.Type.Params.Clear();
                }

                Model.Right.Clear();
                Model.Right.Add("");
                Model.Wrong.Clear();

                change.Commit();
                return;
            }

            Model.Price = Question.InvalidPrice;

            if (IsUpgraded)
            {
                Model.Parameters?.Clear();
            }
            else
            {
                Model.Scenario.Clear();
                Model.Type.Params.Clear();
            }

            Model.Right.Clear();
            Model.Wrong.Clear();

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }
}
