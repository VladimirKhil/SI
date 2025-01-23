using SICore.Contracts;
using SICore.Utils;
using SIData;
using System.Text.Json;

namespace SICore.Special;

/// <summary>
/// Contains default computer persons.
/// </summary>
public static class StoredPersonsRegistry
{
    /// <summary>
    /// Contains default computer persons.
    /// </summary>
    private static StoredPersons? _storedPersons = null;

    internal static StoredPersons StoredPersons
    {
        get
        {
            if (_storedPersons == null)
            {
                var personsJsonStream =
                    System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("SICore.persons.json");

                if (personsJsonStream == null)
                {
                    return new StoredPersons();
                }

                using (personsJsonStream)
                {
                    _storedPersons = JsonSerializer.Deserialize<StoredPersons>(personsJsonStream) ?? new StoredPersons();
                }
            }

            return _storedPersons;
        }
    }

    public static ComputerAccount[] GetDefaultPlayers(ILocalizer localizer, string photoPath)
    {
        var cultureCode = CultureHelper.GetCultureCode(localizer.Culture.Name);

        return StoredPersons.Players.Select(
            player => new ComputerAccount(player)
            {
                Name = player.GetLocalizedName(cultureCode),
                IsMale = player.IsMale,
                Picture = Path.Combine(photoPath, player.Picture)
            })
            .OrderBy(player => player.Name)
            .ToArray();
    }

    public static ComputerAccount[] GetDefaultShowmans(ILocalizer localizer, string photoPath)
    {
        var cultureCode = CultureHelper.GetCultureCode(localizer.Culture.Name);

        return StoredPersons.Showmans.Select(
            showman => new ComputerAccount(showman)
            {
                Name = showman.GetLocalizedName(cultureCode),
                IsMale = showman.IsMale,
                Picture = Path.Combine(photoPath, showman.Picture)
            })
            .OrderBy(showman => showman.Name)
            .ToArray();
    }
}
