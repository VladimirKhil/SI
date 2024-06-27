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

    public event Action<QuestionViewModel, string>? TypeNameChanged;

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
    /// Tries to get question answer options.
    /// </summary>
    public StepParameterViewModel? AnswerOptions
    {
        get
        {
            if (Parameters == null)
            {
                return null;
            }

            Parameters.TryGetValue(QuestionParameterNames.AnswerOptions, out var answerOptionsParameter);
            return answerOptionsParameter;
        }
    }

    public QuestionViewModel(Question question)
        : base(question)
    {
        Right = new AnswersViewModel(this, question.Right, true);
        Wrong = new AnswersViewModel(this, question.Wrong, false);

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
        try
        {
            Parameters?.AddAnswer(new StepParameterViewModel(this, new StepParameter
            {
                Type = StepParameterTypes.Content,
                ContentValue = new List<ContentItem>(new[]
                    {
                    new ContentItem { Type = ContentTypes.Text, Placement = ContentPlacements.Screen, Value = "" }
                })
            }));
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void RemoveComplexAnswer_Executed(object? arg) => Parameters?.RemoveAnswer();

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

        var ownerPackage = OwnerTheme.OwnerRound?.OwnerPackage;

        if (ownerPackage == null)
        {
            return;
        }

        var quest = Model.Clone();
        var newQuestionViewModel = new QuestionViewModel(quest);
        OwnerTheme.Questions.Add(newQuestionViewModel);
        ownerPackage.Document.Navigate.Execute(newQuestionViewModel);
    }

    private void RemoveQuestion_Executed(object? arg)
    {
        var ownerTheme = OwnerTheme;

        if (ownerTheme == null)
        {
            return;
        }

        var ownerDocument = ownerTheme.OwnerRound?.OwnerPackage?.Document;

        if (ownerDocument == null)
        {
            return;
        }

        try
        {
            using var change = ownerDocument.OperationsManager.BeginComplexChange();
            ownerTheme.Questions.Remove(this);
            change.Commit();

            if (ownerDocument?.ActiveNode == this)
            {
                ownerDocument.ActiveNode = ownerTheme;
            }
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void SetQuestionType_Executed(object? arg)
    {
        var typeName = (string?)arg ?? "";
        TypeName = typeName == QuestionTypes.Default ? "?" : typeName;
    }

    private void SetAnswerType_Executed(object? arg)
    {
        try
        {
            var document = OwnerTheme?.OwnerRound?.OwnerPackage?.Document ?? throw new InvalidOperationException("document is undefined");
            
            if (Parameters == null || arg == null)
            {
                return;
            }

            var answerType = (string)arg;

            if (answerType == StepParameterValues.SetAnswerTypeType_Text)
            {
                // Default value; remove parameter
                using var innerChange = document.OperationsManager.BeginComplexChange();

                Parameters.RemoveParameter(QuestionParameterNames.AnswerType);
                Parameters.RemoveParameter(QuestionParameterNames.AnswerOptions);

                innerChange.Commit();
                return;
            }

            using var change = document.OperationsManager.BeginComplexChange();

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
                        new() { Type = ContentTypes.Text, Value = "" },
                    }
                };

                for (var i = 0; i < AppSettings.Default.SelectOptionCount; i++)
                {
                    options.GroupValue.Add(IndexLabelHelper.GetIndexLabel(i), answerOptionGenerator());
                }

                var optionsViewModel = new StepParameterViewModel(this, options);

                Parameters.AddParameter(QuestionParameterNames.AnswerOptions, optionsViewModel);
                Right.ClearOneByOne();
                Right.Add(IndexLabelHelper.GetIndexLabel(0));
                Wrong.ClearOneByOne();
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void SwitchEmpty_Executed(object? arg)
    {
        try
        {
            var document = OwnerTheme?.OwnerRound?.OwnerPackage?.Document ?? throw new InvalidOperationException("document is undefined");

            using var change = document.OperationsManager.BeginComplexChange();

            if (Model.Price == Question.InvalidPrice)
            {
                Model.Price = AppSettings.Default.QuestionBase * ((OwnerTheme?.Questions.IndexOf(this) ?? 0) + 1);

                Parameters!.ClearOneByOne();

                Parameters.AddParameter(QuestionParameterNames.Question, new StepParameterViewModel(this, new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                        {
                            new() { Type = ContentTypes.Text, Value = "" },
                        }
                }));

                Right.ClearOneByOne();
                Right.Add("");
                Wrong.ClearOneByOne();

                change.Commit();
                return;
            }

            Model.Price = Question.InvalidPrice;

            Parameters?.ClearOneByOne();
            TypeName = QuestionTypes.Default;

            Right.ClearOneByOne();
            Wrong.ClearOneByOne();

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }
}
