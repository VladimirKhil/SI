namespace SICore.Network.Configuration
{
    public sealed class ServerConfiguration
    {
        public static readonly ServerConfiguration Default = new ServerConfiguration();

        public int MaxChatMessageLength { get; set; } = 250;
    }
}
