using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Defines SIGame client callback methods.
/// </summary>
public interface ISIOnlineClient
{
	Task GameCreated(GameInfo gameInfo);

	Task GameChanged(GameInfo gameInfo);

	Task GameDeleted(int id);
}
