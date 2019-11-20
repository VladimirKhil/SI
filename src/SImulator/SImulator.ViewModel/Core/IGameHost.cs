using System.ServiceModel;

namespace SImulator.ViewModel.Core
{
    /// <summary>
    /// Сервис, управляющий игрой
    /// </summary>
    public interface IGameHost
    {
        [OperationContract(IsOneWay = true)]
        void OnQuestionSelected(int theme, int question);

        [OperationContract(IsOneWay = true)]
        void OnThemeSelected(int themeIndex);

        [OperationContract(IsOneWay = true)]
        void AskNext();

        [OperationContract(IsOneWay = true)]
        void AskBack();

        [OperationContract(IsOneWay = true)]
        void AskNextRound();

        [OperationContract(IsOneWay = true)]
        void AskBackRound();

        [OperationContract(IsOneWay = true)]
        void AskStop();

        [OperationContract(IsOneWay = true)]
        void OnReady();

        [OperationContract(IsOneWay = true)]
        void OnMediaStart();

        [OperationContract(IsOneWay = true)]
        void OnMediaEnd();

        [OperationContract(IsOneWay = true)]
        void OnMediaProgress(double progress);

        [OperationContract(IsOneWay = true)]
        void OnIntroFinished();

        [OperationContract(IsOneWay = true)]
        void OnRoundThemesFinished();
    }
}
