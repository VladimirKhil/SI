using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core
{
    /// <summary>
    /// Provides media for questions.
    /// </summary>
    public interface IMediaSource
    {
        /// <summary>
        /// Gets media for atom.
        /// </summary>
        /// <param name="atom">Atom containing media.</param>
        IMedia? GetMedia(Atom atom);
    }
}
