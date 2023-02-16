namespace SICore.Network.Contracts;

/// <summary>
/// Defines a primary node.
/// </summary>
public interface IPrimaryNode : INode
{
    /// <summary>
    /// Client unbanned event.
    /// </summary>
    event Action<string> Unbanned;

    /// <summary>
    /// Banned clients identifiers and names.
    /// </summary>
    IReadOnlyDictionary<string, string> Banned { get; }

    /// <summary>
    /// Kicks a client from node.
    /// </summary>
    /// <param name="name">Client name.</param>
    /// <param name="ban">Should the client be banned (kicked forever).</param>
    /// <returns>Kicked client identifier.</returns>
    string Kick(string name, bool ban = false);

    /// <summary>
    /// Unbans a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    void Unban(string clientId);
}
