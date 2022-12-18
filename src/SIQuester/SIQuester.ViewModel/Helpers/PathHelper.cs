using System.Text;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Defines methods for deconding and encoding paths as valid file names.
/// </summary>
/// <remarks>
/// Equation X == DecodePath(EncodePath(X)) should be always true.
/// </remarks>
internal static class PathHelper
{
    /// <summary>
    /// Encodes path as a valid file name.
    /// </summary>
    /// <param name="path">Path to encode.</param>
    /// <returns>Encoded file name.</returns>
    internal static string EncodePath(string path)
    {
        var result = new StringBuilder();

        for (int i = 0; i < path.Length; i++)
        {
            var c = path[i];

            if (c == '%')
            {
                result.Append("%%");
            }
            else if (c == '\\')
            {
                result.Append("%)");
            }
            else if (c == '/')
            {
                result.Append("%(");
            }
            else if (c == ':')
            {
                result.Append("%;");
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Decodes file name as a path.
    /// </summary>
    /// <param name="fileName">File name to decode.</param>
    /// <returns>Decoded path.</returns>
    internal static string DecodePath(string fileName)
    {
        var result = new StringBuilder();

        for (int i = 0; i < fileName.Length; i++)
        {
            var c = fileName[i];

            if (c == '%' && i + 1 < fileName.Length)
            {
                var c1 = fileName[++i];

                if (c1 == '%')
                    result.Append('%');
                else if (c1 == ')')
                    result.Append('\\');
                else if (c1 == '(')
                    result.Append('/');
                else if (c1 == ';')
                    result.Append(':');
                else
                    result.Append(c).Append(c1);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes invalid file name characters from file name.
    /// </summary>
    /// <param name="filename">File name to process.</param>
    internal static string RemoveInvalidFileNameChars(string filename) => string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
}
