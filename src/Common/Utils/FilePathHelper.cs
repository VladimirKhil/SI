using System.Security.Cryptography;
using System.Text;

namespace Utils;

public static class FilePathHelper
{
    private static readonly MD5 Hash = MD5.Create();

    public static string GetSafeFileName(Uri uri) => GetSafeFileName(uri.LocalPath, uri.AbsoluteUri);

    public static string GetSafeFileName(string fileName, string? fileKey = null)
    {
        var extension = Path.GetExtension(fileName);
        var hashedFileName = Hash.ComputeHash(Encoding.UTF8.GetBytes(fileKey ?? fileName));

        var base64String = Convert.ToBase64String(hashedFileName);
        var escapedFileName = ReplaceInvalidFileNameCharactersInBase64(base64String);

        return string.IsNullOrEmpty(extension) ? escapedFileName : Path.ChangeExtension(escapedFileName, extension);
    }

    private static string ReplaceInvalidFileNameCharactersInBase64(string base64String)
    {
        var resultBuilder = new StringBuilder(base64String.Length);

        foreach (var c in base64String)
        {
            switch (c)
            {
                case '/':
                    resultBuilder.Append('_');
                    break;

                case '+':
                    resultBuilder.Append('-');
                    break;

                case '=':
                    // Skip '=' characters
                    break;

                default:
                    resultBuilder.Append(c);
                    break;
            }
        }

        return resultBuilder.ToString();
    }
}
