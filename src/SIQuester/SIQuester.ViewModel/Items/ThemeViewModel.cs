using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class ThemeViewModel: ItemViewModel<Theme>
    {
        public RoundViewModel OwnerRound { get; set; }

        public override IItemViewModel Owner => OwnerRound;

        public ObservableCollection<QuestionViewModel> Questions { get; } = new ObservableCollection<QuestionViewModel>();
        public override ICommand Add { get; protected set; }
        public override ICommand Remove { get; protected set; }
        public ICommand Clone{ get; private set; }
        public ICommand AddQuestion { get; private set; }
        public ICommand SortQuestions { get; private set; }

        public ThemeViewModel(Theme theme)
            : base(theme)
        {
            foreach (var question in theme.Questions)
            {
                Questions.Add(new QuestionViewModel(question) { OwnerTheme = this });
            }

            Questions.CollectionChanged += Questions_CollectionChanged;

            Clone = new SimpleCommand(CloneTheme_Executed);
            Remove = new SimpleCommand(RemoveTheme_Executed);
            Add = AddQuestion = new SimpleCommand(AddQuestion_Executed);
            SortQuestions = new SimpleCommand(SortQuestions_Executed);
        }

        private void Questions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                    {
                        if (Questions[i].OwnerTheme != null)
                        {
                            throw new Exception(Resources.ErrorInsertingBindedQuestion);
                        }

                        Questions[i].OwnerTheme = this;
                        Model.Questions.Insert(i, Questions[i].Model);
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    var temp = Model.Questions[e.OldStartingIndex];
                    Model.Questions.Insert(e.NewStartingIndex, temp);
                    Model.Questions.RemoveAt(e.OldStartingIndex + (e.NewStartingIndex < e.OldStartingIndex ? 1 : 0));
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (QuestionViewModel question in e.OldItems)
                    {
                        question.OwnerTheme = null;
                        Model.Questions.RemoveAt(e.OldStartingIndex);

                        OwnerRound?.OwnerPackage?.Document?.ClearLinks(question);
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Model.Questions.Clear();
                    foreach (QuestionViewModel question in Questions)
                    {
                        question.OwnerTheme = this;
                        Model.Questions.Add(question.Model);
                    }
                    break;
            }
        }

        private void CloneTheme_Executed(object arg)
        {
            var newTheme = Model.Clone() as Theme;
            var newThemeViewModel = new ThemeViewModel(newTheme);
            OwnerRound.Themes.Add(newThemeViewModel);
            OwnerRound.OwnerPackage.Document.Navigate.Execute(newThemeViewModel);
        }

        private void RemoveTheme_Executed(object arg)
        {
            if (OwnerRound == null)
            {
                return;
            }

            OwnerRound.Themes.Remove(this);
        }

        private void AddQuestion_Executed(object arg)
        {
            try
            {
                var document = OwnerRound.OwnerPackage.Document;
                var price = Questions.Count == 0 ? AppSettings.Default.QuestionBase : -1;

                if (price == -1)
                {
                    var n = Questions.Count;
                    if (n > 1)
                    {
                        var add = Questions[1].Model.Price - Questions[0].Model.Price;
                        price = Math.Max(0, Questions[n - 1].Model.Price + add);
                    }
                    else if (n > 0)
                    {
                        price = Questions[0].Model.Price * 2;
                    }
                    else if (OwnerRound.Model.Type == RoundTypes.Final)
                    {
                        price = 0;
                    }
                    else
                    {
                        price = 100;
                    }
                }

                var question = QDocument.CreateQuestion(price);

                var questionViewModel = new QuestionViewModel(question);
                Questions.Add(questionViewModel);

                QDocument.ActivatedObject = questionViewModel.Scenario.FirstOrDefault();
                document.Navigate.Execute(questionViewModel);
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
            }
        }

        private void SortQuestions_Executed(object arg)
        {
            try
            {
                var document = OwnerRound.OwnerPackage.Document;
                document.BeginChange();
                try
                {
                    for (int i = 1; i < Questions.Count; i++)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (Questions[i].Model.Price < Questions[j].Model.Price)
                            {
                                Questions.Move(i, j);
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    document.CommitChange();
                }
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
            }
        }

        protected override void UpdateCosts(CostSetter costSetter)
        {
            var document = OwnerRound.OwnerPackage.Document;
            document.BeginChange();
            try
            {
                UpdateCostsCore(costSetter);
            }
            finally
            {
                document.CommitChange();
            }
        }

        public void UpdateCostsCore(CostSetter costSetter)
        {
            for (int i = 0; i < Questions.Count; i++)
            {
                Questions[i].Model.Price = costSetter.BaseValue + costSetter.Increment * i;
            }
        }
    }
}
