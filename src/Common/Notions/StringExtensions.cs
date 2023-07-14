using System.Text;

namespace Notions;

/// <summary>
/// Provides helper methods for manipulating strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Translits a string (converts its non-latin to cirresponding latic characters).
    /// </summary>
    /// <param name="str">Input string.</param>
    /// <returns>Translitted value.</returns>
    public static string Translit(this string str)
    {
        var res = new StringBuilder();
        var length = str.Length;
        for (var i = 0; i < length; i++)
        {
            switch (str[i])
            {
                case 'а':
                case 'А':
                    res.Append('a');
                    break;
                case 'б':
                case 'Б':
                    res.Append('b');
                    break;
                case 'в':
                case 'В':
                    res.Append('v');
                    break;
                case 'г':
                case 'Г':
                    res.Append('g');
                    break;
                case 'д':
                case 'Д':
                    res.Append('d');
                    break;
                case 'е':
                case 'Е':
                    res.Append('e');
                    break;
                case 'ё':
                case 'Ё':
                    res.Append("jo");
                    break;
                case 'ж':
                case 'Ж':
                    res.Append("zh");
                    break;
                case 'з':
                case 'З':
                    res.Append('z');
                    break;
                case 'и':
                case 'И':
                    res.Append('i');
                    break;
                case 'й':
                case 'Й':
                    res.Append('j');
                    break;
                case 'к':
                case 'К':
                    res.Append('k');
                    break;
                case 'л':
                case 'Л':
                    res.Append('l');
                    break;
                case 'м':
                case 'М':
                    res.Append('m');
                    break;
                case 'н':
                case 'Н':
                    res.Append('n');
                    break;
                case 'о':
                case 'О':
                    res.Append('o');
                    break;
                case 'п':
                case 'П':
                    res.Append('p');
                    break;
                case 'р':
                case 'Р':
                    res.Append('r');
                    break;
                case 'с':
                case 'С':
                    res.Append('s');
                    break;
                case 'т':
                case 'Т':
                    res.Append('t');
                    break;
                case 'у':
                case 'У':
                    res.Append('u');
                    break;
                case 'ф':
                case 'Ф':
                    res.Append('f');
                    break;
                case 'х':
                case 'Х':
                    res.Append('h');
                    break;
                case 'ц':
                case 'Ц':
                    res.Append('c');
                    break;
                case 'ч':
                case 'Ч':
                    res.Append("ch");
                    break;
                case 'ш':
                case 'Ш':
                    res.Append("sh");
                    break;
                case 'щ':
                case 'Щ':
                    res.Append("sch");
                    break;
                case 'ы':
                case 'Ы':
                    res.Append('y');
                    break;
                case 'э':
                case 'Э':
                    res.Append('e');
                    break;
                case 'ю':
                case 'Ю':
                    res.Append("ju");
                    break;
                case 'я':
                case 'Я':
                    res.Append("ja");
                    break;

                default:
                    if (str[i] >= 'a' && str[i] <= 'z')
                        res.Append(str[i]);
                    else if (str[i] >= 'A' && str[i] <= 'Z')
                        res.Append((char)(str[i] - 'A' + 'a'));
                    break;
            }
        }

        return res.ToString();
    }

    public static string DigitPart(this string s)
    {
        var res = new StringBuilder();
        int length = s.Length;
        for (var i = 0; i < length; i++)
        {
            if (char.IsDigit(s[i]))
            {
                res.Append(s[i]);
            }
        }

        return res.ToString();
    }

    public static string NotDigitPart(this string s)
    {
        var res = new StringBuilder();
        int length = s.Length;

        for (int i = 0; i < length; i++)
        {
            if (!char.IsDigit(s[i]))
            {
                res.Append(s[i]);
            }
        }

        return res.ToString();
    }

    /// <summary>
    /// Упрощение строки
    /// </summary>
    /// <param name="s">Входная строка</param>
    /// <returns>Строка, содержащая только смысловую информацию (буквы и цифры)
    /// Все символы переводятся в нижний регистр
    /// Если в результате получается пустая строка, то упрощение невозможно
    /// В этом случае возвращается исходная строка</returns>
    public static string Simplify(this string s)
    {
        var length = s.Length;
        var res = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            if (char.IsLetterOrDigit(s[i]))
            {
                res.Append(char.ToLower(s[i]));
            }
        }

        return res.ToString().Replace('ё', 'е');
    }

    /// <summary>
    /// Дополнение строки символами перевода строки при необходимости и удаление лишних символов перевода строки
    /// </summary>
    /// <param name="str">Входная строка</param>
    /// <returns>Результат</returns>
    public static string ProlongString(this string str)
    {
        if (str.Length < 2)
        {
            return str + Environment.NewLine;
        }

        var length = str.Length - 2;

        while (length >= 0 && str.Substring(length, 2) == Environment.NewLine)
        {
            length -= 2;
        }

        return string.Concat(str.AsSpan(0, length + 2), Environment.NewLine);
    }

    /// <summary>
    /// Завершение строки точкой
    /// </summary>
    /// <param name="s">Строка</param>
    /// <returns>Результат</returns>
    public static string EndWithPoint(this string s)
    {
        var res = ClearPoints(s);

        if (res.Length == 0)
        {
            return "";
        }

        if (Uri.IsWellFormedUriString(s, UriKind.Absolute))
        {
            return res;
        }

        var last = res[^1];

        if (last != '?' && last != '!' && last != '…')
        {
            res += ".";
        }

        return res;
    }

    /// <summary>
    /// Удаление точек и пробелов в конце
    /// </summary>
    /// <param name="s">Строка</param>
    /// <returns>Результат</returns>
    public static string ClearPoints(this string s)
    {
        if (s.Length == 0)
        {
            return "";
        }

        var length = s.Length - 1;

        while (length > 0 && (s[length] == '.' || char.IsWhiteSpace(s[length])))
        {
            length--;
        }

        return s[..(length + 1)];
    }

    /// <summary>
    /// Делает первую букву прописной
    /// </summary>
    /// <param name="s">Строка</param>
    /// <returns>Результат</returns>
    public static string GrowFirstLetter(this string s)
    {
        if (s.Length == 0)
        {
            return "";
        }

        if (Uri.TryCreate(s, UriKind.Absolute, out Uri? uri) && uri.IsWellFormedOriginalString())
        {
            return s;
        }

        var i = 0;
        var res = new StringBuilder();

        while (i < s.Length && !char.IsLetter(s[i]) && !char.IsNumber(s[i]))
        {
            res.Append(s[i++]);
        }

        if (i < s.Length)
        {
            res.Append(char.ToUpper(s[i]));

            if (i + 1 < s.Length)
            {
                res.Append(s.AsSpan(i + 1));
            }
        }

        return res.ToString();
    }

    public static string FullTrim(this string s)
    {
        s = s.Trim();

        if (s.Length == 0)
        {
            return s;
        }

        var res = new StringBuilder(s[0].ToString());
        var length = s.Length;

        for (var i = 1; i < length; i++)
        {
            if (s[i] == '\n' || !char.IsWhiteSpace(s[i]) || !char.IsWhiteSpace(s[i - 1]))
            {
                res.Append(s[i]);
            }
        }

        return res.ToString();
    }

    /// <summary>
    /// Обрубает строку многоточием, если она слишком длинная
    /// </summary>
    /// <param name="str">Входная строка</param>
    /// <param name="n">Пределная длина</param>
    /// <returns>Результирующая обрезанная строка</returns>
    public static string LeaveFirst(this string str, int n) => (str.Length > n) ? string.Concat(str.AsSpan(n - 1), "…") : str;

    public static string FormatNumber(this string s, bool format = false)
    {
        if (s.Length == 0)
        {
            return "";
        }

        if (!format && !char.IsDigit(s[0]))
        {
            return s[0] + FormatNumber(s[1..]);
        }

        if (s.Length <= 4)
        {
            return s;
        }

        return $"{FormatNumber(s[..^3], true)} {s[^3..]}";
    }

    /// <summary>
    /// Formats the string for better reading (fixes quotes, removes whitespaces etc.).
    /// </summary>
    public static string Wikify(this string s)
    {
        var length = s.Length;
        var res = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            if (s[i] == '-' && i > 0 && char.IsWhiteSpace(s[i - 1]) && i < length - 1 && char.IsWhiteSpace(s[i + 1]))
            {
                res.Append('—');
            }
            else if (s[i] == '.')
            {
                if (i <= length - 3 && s.Substring(i, 3) == "...")
                {
                    res.Append('…');
                    i += 2;
                }
                else
                {
                    res.Append('.');
                }
            }
            else if (s[i] == '"')
            {
                if (i == 0 || char.IsWhiteSpace(s[i - 1]) || i < length - 1 && (char.IsLetter(s[i + 1]) || char.IsDigit(s[i + 1])))
                {
                    res.Append('«');
                }
                else if (i == length - 1 ||
                    !char.IsLetter(s[i + 1]) && !char.IsDigit(s[i + 1]) ||
                    i > 0 && (char.IsLetter(s[i - 1]) ||
                    char.IsDigit(s[i - 1])))
                {
                    res.Append('»');
                }
                else
                {
                    res.Append(s[i]);
                }
            }
            else
            {
                res.Append(s[i]);
            }
        }

        return res.ToString().Trim();
    }

    /// <summary>
    /// Correctly shortens the string. Respects character surrogates.
    /// </summary>
    /// <param name="s">The string to shorten.</param>
    /// <param name="maxLength">Maximum allowed string length.</param>
    /// <param name="ellipsis">Shorten line ending.</param>
    /// <returns>If string length is less than or equal to {maxLength}, returns original string.
    /// Otherwise returnes substring of {s} which length is less than {maxLength}</returns>
    public static string Shorten(this string s, int maxLength, string ellipsis = "")
    {
        if (maxLength == 0 || s.Length == 0)
        {
            return "";
        }

        if (s.Length <= maxLength)
        {
            return s;
        }

        // Surrogates have high component first
        return s[..(maxLength - (char.IsHighSurrogate(s[maxLength - 1]) ? 1 : 0))] + ellipsis;
    }
}
