using SIUI.Model;
using System.Threading.Tasks;

namespace SIUI.ViewModel
{
    /// <summary>
    /// Информация о вопросе
    /// </summary>
    public sealed class QuestionInfoViewModel : ViewModelBase<QuestionInfo>
    {
        private QuestionInfoStages _state = QuestionInfoStages.None;

        /// <summary>
        /// Цена вопроса
        /// </summary>
        public int Price
        {
            get { return _model.Price; }
            set { _model.Price = value; OnPropertyChanged(); }
        }

        public QuestionInfoStages State
        {
            get { return _state; }
            set { _state = value; OnPropertyChanged(); }
        }

        public QuestionInfoViewModel()
        {
            
        }

        public QuestionInfoViewModel(QuestionInfo questionInfo) : this()
        {
            _model = questionInfo;
        }

        internal async void SilentFlashOut()
        {
            await Task.Delay(500);

            _state = QuestionInfoStages.None;
            _model.Price = -1;
        }
    }
}
