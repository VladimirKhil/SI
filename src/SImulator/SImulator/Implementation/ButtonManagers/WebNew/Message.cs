namespace SImulator.Implementation.ButtonManagers.WebNew;

public sealed class Message
{
    /// <summary>
    /// Message text.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Is the message a system message (not a chat message).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Is the message a private message.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Message sender.
    /// </summary>
    public string Sender { get; set; } = "";

    /// <summary>
    /// Message reveiver.
    /// </summary>
    public string? Receiver { get; set; }
}
