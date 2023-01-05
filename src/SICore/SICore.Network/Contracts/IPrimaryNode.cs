namespace SICore.Network.Contracts;

/// <summary>
/// Defines a master node.
/// </summary>
public interface IPrimaryNode : INode
{
    /// <summary>
    /// Client unbanned event.
    /// </summary>
    event Action<string> Unbanned;

    /// <summary>
    /// Banned clients IPs and names.
    /// </summary>
    IReadOnlyDictionary<string, string> Banned { get; }

    /// <summary>
    /// Kicks a client from node.
    /// </summary>
    /// <param name="name">Client name.</param>
    /// <param name="ban">Should the client be banned (kicked forever).</param>
    /// <returns>Kicked client IP.</returns>
    string Kick(string name, bool ban = false);

    /// <summary>
    /// Unbans a client.
    /// </summary>
    /// <param name="name">Client name.</param>
    void Unban(string name);
}
