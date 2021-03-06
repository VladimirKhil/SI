using SIPackages;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class QuestionViewModel: ItemViewModel<Question>
    {
        public ThemeViewModel OwnerTheme { get; set; }

        public override IItemViewModel Owner => OwnerTheme;

        public AnswersViewModel Right { get; private set; }
        public AnswersViewModel Wrong { get; private set; }
        public ScenarioViewModel Scenario { get; private set; }

        public QuestionTypeViewModel Type { get; private set; }

        public SimpleCommand AddWrongAnswers { get; private set; }

        public override ICommand Add
        {
            get => null;
            protected set { }
        }

        public override ICommand Remove { get; protected set; }
        public ICommand Clone { get; private set; }

        public ICommand SetQuestionType { get; private set; }

        public QuestionViewModel(Question question)
            : base(question)
        {
            Type = new QuestionTypeViewModel(question.Type);

            Right = new AnswersViewModel(this, question.Right, true);
            Wrong = new AnswersViewModel(this, question.Wrong, false);
            Scenario = new ScenarioViewModel(this, question.Scenario);

            BindHelper.Bind(Right, question.Right);
            BindHelper.Bind(Wrong, question.Wrong);

            AddWrongAnswers = new SimpleCommand(AddWrongAnswers_Executed);

            Clone = new SimpleCommand(CloneQuestion_Executed);
            Remove = new SimpleCommand(RemoveQuestion_Executed);

            SetQuestionType = new SimpleCommand(SetQuestionType_Executed);

            Wrong.CollectionChanged += Wrong_CollectionChanged;
        }

        private void Wrong_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AddWrongAnswers.CanBeExecuted = Model.Wrong.Count == 0;
        }

        private void AddWrongAnswers_Executed(object arg)
        {
            QDocument.ActivatedObject = Wrong;
            Wrong.Add("");
        }

        private void CloneQuestion_Executed(object arg)
        {
            if (OwnerTheme != null)
            {
                var quest = Model.Clone();
                var newQuestionViewModel = new QuestionViewModel(quest);
                OwnerTheme.Questions.Add(newQuestionViewModel);
                OwnerTheme.OwnerRound.OwnerPackage.Document.Navigate.Execute(newQuestionViewModel);
            }
        }

        private void RemoveQuestion_Executed(object arg) => OwnerTheme?.Questions.Remove(this);

        private void SetQuestionType_Executed(object arg)
        {
            Type.Model.Name = (string)arg;
        }
    }
}
