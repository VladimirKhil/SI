namespace SImulator.ViewModel.Contracts;

public interface ICommandExecutor
{
    void OnStage(string stageName);

    void AskStake(string connectionId, int maximum);

    void AskTextAnswer();

    void AskOralAnswer(string connectionId);

    void Cancel();
}
