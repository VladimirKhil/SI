using SIPackages.Core;

namespace SICore
{
    /// <summary>
    /// Allows to get file data by file name.
    /// </summary>
    public interface IFilesManager
    {
        /// <summary>
        /// Gets file data by file name.
        /// </summary>
        /// <param name="file">File name.</param>
        /// <returns>File data.</returns>
        StreamInfo GetFile(string file);
    }
}
