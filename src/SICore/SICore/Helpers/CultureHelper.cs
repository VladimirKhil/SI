namespace SICore.Utils;

public static class CultureHelper
{
    public static string GetCultureCode(string culture)
    {
        // CultureInfo.TwoLetterISOLanguageName does not work when app is running in Globalization invariant mode
        var hyphenIndex = culture.IndexOf('-');
        return hyphenIndex > -1 ? culture.Substring(0, hyphenIndex) : culture;
    }
}
