namespace SIGame.ViewModel.PackageSources;

public sealed class PackageSourceKey
{
    public PackageSourceTypes Type { get; set; }
    public string Data { get; set; }
    public int ID { get; set; }
    public string Name { get; set; }
    public string PackageID { get; set; }
}
