using System.Text.RegularExpressions;

namespace ZipUtils;

/// <summary>
/// Provides API for calculating file names hashes.
/// </summary>
internal static class HashHelper
{
    private const int MaxExtensionLength = 4;

    /// <summary>
    /// Creates a unqiue value hash. Used when an original value is too long to be used as a file name.
    /// Keeps original file extension if it is safe.
    /// </summary>
    /// <param name="value">Value to hash.</param>
    /// <returns>Hashed value.</returns>
    /// <remarks>I do not remember where did these constants come from. So it has some historical logic.</remarks>
    internal static string CalculateHash(string value)
    {
        var hashedValue = 3074457345618258791ul;

        for (var i = 0; i < value.Length; i++)
        {
            hashedValue += value[i];
            hashedValue *= 3074457345618258799ul;
        }

        var result = hashedValue.ToString("X2");

        var extensionIndex = value.LastIndexOf('.');

        if (extensionIndex > -1)
        {
            var extensionLength = Math.Min(MaxExtensionLength, value.Length - extensionIndex - 1);
            result += '.' + Regex.Replace(value.Substring(extensionIndex + 1, extensionLength), "[^a-zA-Z0-9]+", "");
        }

        return result;
    }
}
