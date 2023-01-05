namespace SICore.Network.Configuration;

/// <summary>
/// Defines a node configuration.
/// </summary>
public sealed class NodeConfiguration
{
    public static readonly NodeConfiguration Default = new();

    public int MaxChatMessageLength { get; set; } = 250;
}
