using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Defines SIGame client callback methods.
/// </summary>
public interface ISIOnlineClient
{
	Task Joined(string name);

	Task Leaved(string name);

	Task Say(string user, string text);

	Task GameCreated(GameInfo gameInfo);

	Task GameChanged(GameInfo gameInfo);

	Task GameDeleted(int id);

	Task Receive(Message message);

	Task Disconnect();
}
