using SICore.Contracts;
using SICore.Models;
using SIGame.ViewModel.Properties;

namespace SIGame.ViewModel.Services;

/// <inheritdoc cref="IAvatarHelper" />
internal sealed class AvatarHelper : IAvatarHelper
{
    /// <summary>
    /// Maximum avatar size in bytes.
    /// </summary>
    private const int MaxAvatarSize = 1024 * 1024;

    private readonly string _avatarFolder;

    public AvatarHelper(string avatarFolder) => _avatarFolder = avatarFolder;

    public bool FileExists(string fileName) => File.Exists(Path.Combine(_avatarFolder, fileName));
        
    public (ErrorCode, string)? ExtractAvatarData(string base64data, string fileName)
    {
        var imageDataSize = ((base64data.Length * 3) + 3) / 4 -
            (base64data.Length > 0 && base64data[^1] == '=' ?
                base64data.Length > 1 && base64data[^2] == '=' ?
                    2 : 1 : 0);

        if (imageDataSize > MaxAvatarSize)
        {
            return (ErrorCode.AvatarTooBig, Resources.AvatarTooBig);
        }

        var imageData = new byte[imageDataSize];

        if (!Convert.TryFromBase64String(base64data, imageData, out var bytesWritten))
        {
            return (ErrorCode.InvalidAvatar, Resources.InvalidAvatarData);
        }

        Array.Resize(ref imageData, bytesWritten);

        File.WriteAllBytes(Path.Combine(_avatarFolder, fileName), imageData);

        return null;
    }

    public void AddFile(string sourceFilePath, string fileName) => File.Copy(sourceFilePath, Path.Combine(_avatarFolder, fileName));
}
