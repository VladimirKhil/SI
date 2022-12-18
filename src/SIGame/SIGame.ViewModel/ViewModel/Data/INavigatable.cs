namespace SIGame.ViewModel.Data;

public interface INavigatable
{
    event Action<ContentBox> Navigate;

    void OnNavigatedFrom(object data);
}
