namespace SIQuester;

/// <summary>
/// Коллекция вспомогательных методов
/// </summary>
internal static class Utils
{
    /// <summary>
    /// Совпадает ли число открывающих и закрывающих скобок в тексте
    /// </summary>
    /// <param name="text">Входной текст</param>
    /// <returns>Совпадает ли число открывающих и закрывающих скобок в тексте</returns>
    internal static bool ValidateTextBrackets(this string text)
    {
        if (text == null)
        {
            return true;
        }

        var total = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(':
                    total++;
                    break;

                case ')':
                    if (total > 0)
                    {
                        total--;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }

        return total == 0;
    }
}
