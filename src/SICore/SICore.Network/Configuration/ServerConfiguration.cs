namespace SICore.Network.Configuration
{
    public sealed class ServerConfiguration
    {
        public static readonly ServerConfiguration Default = new();

        public int MaxChatMessageLength { get; set; } = 250;
    }
}
