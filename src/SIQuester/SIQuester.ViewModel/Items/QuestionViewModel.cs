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

    public ICommand AddComplexAnswer { get; private set; }

    public ICommand RemoveComplexAnswer { get; private set; }

    public SimpleCommand AddWrongAnswers { get; private set; }

    public override ICommand Add
    {
        get => null;
        protected set { }
    }

    public override ICommand Remove { get; protected set; }

    public ICommand Clone { get; private set; }

    public ICommand SetQuestionType { get; private set; }

    public ICommand SwitchEmpty { get; private set; }

    public QuestionViewModel(Question question)
        : base(question)
    {
        Type = new QuestionTypeViewModel(question.Type);

        Right = new AnswersViewModel(this, question.Right, true);
        Wrong = new AnswersViewModel(this, question.Wrong, false);
        Scenario = new ScenarioViewModel(this, question.Scenario);

        BindHelper.Bind(Right, question.Right);
        BindHelper.Bind(Wrong, question.Wrong);

        AddComplexAnswer = new SimpleCommand(AddComplexAnswer_Executed);
        RemoveComplexAnswer = new SimpleCommand(RemoveComplexAnswer_Executed);
        AddWrongAnswers = new SimpleCommand(AddWrongAnswers_Executed);

        Clone = new SimpleCommand(CloneQuestion_Executed);
        Remove = new SimpleCommand(RemoveQuestion_Executed);

        SetQuestionType = new SimpleCommand(SetQuestionType_Executed);
        SwitchEmpty = new SimpleCommand(SwitchEmpty_Executed);

        Wrong.CollectionChanged += Wrong_CollectionChanged;
    }

    private void Wrong_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        AddWrongAnswers.CanBeExecuted = Model.Wrong.Count == 0;

    private void AddComplexAnswer_Executed(object? arg)
    {
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
        if (OwnerTheme != null)
        {
            var quest = Model.Clone();
            var newQuestionViewModel = new QuestionViewModel(quest);
            OwnerTheme.Questions.Add(newQuestionViewModel);
            OwnerTheme.OwnerRound.OwnerPackage.Document.Navigate.Execute(newQuestionViewModel);
        }
    }

    private void RemoveQuestion_Executed(object? arg) => OwnerTheme?.Questions.Remove(this);

    private void SetQuestionType_Executed(object? arg) => Type.Model.Name = (string)arg;

    private void SwitchEmpty_Executed(object? arg)
    {
        var document = OwnerTheme.OwnerRound.OwnerPackage.Document;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            if (Model.Price == Question.InvalidPrice)
            {
                Model.Price = AppSettings.Default.QuestionBase * ((OwnerTheme?.Questions.IndexOf(this) ?? 0) + 1);
                Model.Scenario.Clear();
                Model.Scenario.Add(new Atom());
                Model.Type.Params.Clear();
                Model.Right.Clear();
                Model.Right.Add("");
                Model.Wrong.Clear();

                change.Commit();
                return;
            }

            Model.Price = Question.InvalidPrice;
            Model.Scenario.Clear();
            Model.Type.Params.Clear();
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
