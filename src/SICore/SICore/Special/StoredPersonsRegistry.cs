using Newtonsoft.Json;
using SICore.BusinessLogic;
using SICore.Utils;
using System.IO;
using System.Linq;

namespace SICore.Special
{
    public static class StoredPersonsRegistry
    {
        /// <summary>
        /// Коллекция компьютерных участников по умолчанию
        /// </summary>
        private static StoredPersons _storedPersons = null;

        internal static StoredPersons StoredPersons
        {
            get
            {
                if (_storedPersons == null)
                {
                    var serializer = new JsonSerializer();

                    using var personsJsonStream =
                        System.Reflection.Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("SICore.persons.json");

                    using var streamReader = new StreamReader(personsJsonStream);
                    using var reader = new JsonTextReader(streamReader);

                    _storedPersons = serializer.Deserialize<StoredPersons>(reader);
                }

                return _storedPersons;
            }
        }

        public static ComputerAccount[] GetDefaultPlayers(ILocalizer localizer, string photoPath)
        {
            var cultureCode = CultureHelper.GetCultureCode(localizer.Culture.Name);

            return StoredPersons.Players
                .Select(player => new ComputerAccount(player)
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

            var r = StoredPersons.Showmans.Select(
                showman => new ComputerAccount(showman)
                {
                    Name = showman.GetLocalizedName(cultureCode),
                    IsMale = showman.IsMale,
                    Picture = Path.Combine(photoPath, showman.Picture)
                })
                .OrderBy(showman => showman.Name)
                .ToArray();

            return r;
        }
    }
}
