using System.Text;

namespace SIQuester.ViewModel.Helpers
{
    internal static class PathHelper
    {
        internal static string EncodePath(string path)
        {
            var result = new StringBuilder();

            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '%')
                    result.Append("%%");
                else if (c == '\\')
                    result.Append("%)");
                else if (c == '/')
                    result.Append("%(");
                else if (c == ':')
                    result.Append("%;");
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        internal static string DecodePath(string path)
        {
            var result = new StringBuilder();

            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (c == '%' && i + 1 < path.Length)
                {
                    var c1 = path[++i];
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
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}
