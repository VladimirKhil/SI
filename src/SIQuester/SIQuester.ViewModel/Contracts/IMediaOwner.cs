using SIPackages.Core;

namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Defines an object which could provide access to some media data.
/// </summary>
public interface IMediaOwner
{
    /// <summary>
    /// Loads media and provides access to it.
    /// </summary>
    ValueTask<IMedia?> LoadMediaAsync(CancellationToken cancellationToken = default);
}
