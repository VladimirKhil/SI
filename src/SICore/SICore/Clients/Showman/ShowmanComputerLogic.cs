using SICore.Clients.Showman;
using System.Linq;

namespace SICore
{
    /// <summary>
    /// Логика ведущего-компьютера
    /// </summary>
    internal sealed class ShowmanComputerLogic : ViewerComputerLogic, IShowman
    {
        //private readonly ViewerActions _viewerActions;
        //private readonly ViewerData _data;

        public ShowmanComputerLogic(ViewerData data, ViewerActions viewerActions, ComputerAccount computerAccount)
            : base(data, viewerActions, computerAccount)
        {
            //_viewerActions = viewerActions;
            //_data = data;
        }

        internal void ScheduleExecution(ShowmanTasks task, double taskTime) => ScheduleExecution((int)task, 0, taskTime);

        protected override void ExecuteTask(int taskId, int arg)
        {
            var task = (ShowmanTasks)taskId;
            switch (task)
            {
                case ShowmanTasks.AnswerFirst:
                    AnswerFirst();
                    break;

                case ShowmanTasks.AnswerNextStake:
                    AnswerNextStake();
                    break;

                case ShowmanTasks.AnswerRight:
                    AnswerRight();
                    break;

                case ShowmanTasks.AnswerNextToDelete:
                    AnswerNextToDelete();
                    break;

                default:
                    break;
            }
        }

        private void SelectPlayer(string message)
        {
            int num = _data.Players.Count(p => p.CanBeSelected);
            int i = _data.Rand.Next(num);
            while (i < _data.Players.Count && !_data.Players[i].CanBeSelected)
            {
                i++;
            }

            _viewerActions.SendMessage(message, i.ToString());
        }

        private void AnswerNextToDelete() => SelectPlayer(Messages.NextDelete);

        private void AnswerNextStake() => SelectPlayer(Messages.Next);

        private void AnswerFirst() => SelectPlayer(Messages.First);

        private void AnswerRight()
        {
            bool right = false;

            foreach (var s in _data.PersonDataExtensions.Right)
            {
                right = AnswerChecker.IsAnswerRight(_data.PersonDataExtensions.Answer, s);
                if (right)
                {
                    break;
                }
            }

            _viewerActions.SendMessage(Messages.IsRight, right ? "+" : "-");
        }

        #region ShowmanInterface Members

        public void StarterChoose() => ScheduleExecution(ShowmanTasks.AnswerFirst, 10 + _data.Rand.Next(10));

        public void FirstStake() => ScheduleExecution(ShowmanTasks.AnswerNextStake, 10 + _data.Rand.Next(10));

        public void IsRight() => ScheduleExecution(ShowmanTasks.AnswerRight, 10 + _data.Rand.Next(10));

        public void FirstDelete() => ScheduleExecution(ShowmanTasks.AnswerNextToDelete, 10 + _data.Rand.Next(10));

        public void ChangeSum()
        {

        }

        #endregion

        public void OnInitialized()
        {
            ((PersonAccount)_data.Me).BeReadyCommand.Execute(null);
        }

        public void ClearSelections(bool full = false)
        {

        }


        public void ChooseQuest()
        {
            
        }

        public void Cat()
        {
            
        }

        public void Stake()
        {
            
        }

        public void ChooseFinalTheme()
        {
            
        }

        public void FinalStake()
        {
            
        }

        public void CatCost()
        {
            
        }

        public void Table()
        {
            
        }

        public void FinalThemes()
        {
            
        }
    }
}
