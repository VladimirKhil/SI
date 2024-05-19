using SIGame.ViewModel.PackageSources;

namespace SIGame.ViewModel.Contracts;

public interface IPackageSelector
{
    void SelectPackageSource(PackageSource packageSource);
}
