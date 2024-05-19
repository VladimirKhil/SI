namespace SIGame.ViewModel.Models;

public sealed record SIStorageParameters
{
    public int StorageIndex { get; set; }

    public bool IsRandom { get; set; }
}
