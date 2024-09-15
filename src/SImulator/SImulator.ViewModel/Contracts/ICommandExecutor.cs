namespace SImulator.ViewModel.Contracts;

public interface ICommandExecutor
{
    void OnStage(string stageName);

    void AskTextAnswer();
}
