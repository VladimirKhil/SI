using SICore.BusinessLogic;
using SICore.Network.Clients;

namespace SICore
{
    public sealed class GameActions
    {
        private readonly GameData _gameData;
        private readonly ILocalizer LO;

        public Client Client { get; }

        public GameActions(Client client, GameData gameData, ILocalizer localizer)
        {
            Client = client;
            _gameData = gameData;
            LO = localizer;
        }
    }
}
