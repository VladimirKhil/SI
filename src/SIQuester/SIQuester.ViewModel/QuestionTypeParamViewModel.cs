using SIPackages;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class QuestionTypeParamViewModel : ModelViewBase
    {
        public QuestionTypeParam Model { get; }

        public QuestionTypeViewModel Owner { get; internal set; }

        public ICommand AddQuestionTypeParam => Owner?.AddQuestionTypeParam;

        public SimpleCommand RemoveQuestionTypeParam { get; private set; }

        public QuestionTypeParamViewModel(QuestionTypeParam item)
        {
            Model = item;

            Init();
        }

        public QuestionTypeParamViewModel()
        {
            Model = new QuestionTypeParam();

            Init();
        }

        private void Init()
        {
            RemoveQuestionTypeParam = new SimpleCommand(RemoveQuestionTypeParam_Executed);
        }

        private void RemoveQuestionTypeParam_Executed(object arg)
        {
            Owner.Params.Remove(this);
        }
    }
}
