using Notions;
using SICore.PlatformSpecific;
using SIData;

namespace SICore.Extensions;

/// <summary>
/// Provides extention method for <see cref="ComputerAccount" /> class.
/// </summary>
public static class ComputerAccountExtensions
{
    /// <summary>
    /// Sets an avatar for a computer account (if avatar has noot been defined yet).
    /// </summary>
    /// <param name="computerAccount">Computer account to process.</param>
    /// <param name="photoUri">Avatar folder path.</param>
    public static void SetPicture(this ComputerAccount computerAccount, string avatarFolderPath)
    {
        if (!string.IsNullOrEmpty(computerAccount.Picture) && CoreManager.Instance.FileExists(computerAccount.Picture))
        {
            return;
        }

        if (string.IsNullOrEmpty(computerAccount.Name))
        {
            return;
        }

        computerAccount.Picture = Path.Combine(avatarFolderPath, computerAccount.Name.Translit()) + ".jpg";
    }
}
