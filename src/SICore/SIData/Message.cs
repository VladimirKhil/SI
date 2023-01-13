using System.Xml;

namespace SIData;

/// <summary>
/// Defines a game message.
/// </summary>
public readonly struct Message
{
    /// <summary>
    /// Message text arguments separator.
    /// </summary>
    public const char ArgsSeparatorChar = '\n';

    /// <summary>
    /// Message text arguments separator as a string.
    /// </summary>
    public const string ArgsSeparator = "\n";

    private static readonly XmlWriterSettings Settings = new();

    public static readonly Message Empty = new();

    /// <summary>
    /// Message text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Is the message a system message (not a chat message).
    /// </summary>
    public bool IsSystem { get; }

    /// <summary>
    /// Is the message a private message.
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    /// Message sender.
    /// </summary>
    public string Sender { get; }

    /// <summary>
    /// Message reveiver.
    /// </summary>
    public string? Receiver { get; }

    static Message()
    {
        Settings.OmitXmlDeclaration = true;
        Settings.Encoding = System.Text.Encoding.UTF8;
    }

    public Message(string text, string sender, string? receiver = null, bool isSystem = true, bool isPrivate = false)
    {
        Text = text;
        Sender = sender;
        Receiver = receiver;
        IsSystem = isSystem;
        IsPrivate = isPrivate;
    }

    public override string ToString() => Text;

    public static Message ReadXml(XmlReader reader)
    {
        bool isSystem = true, isPrivate = false;

        var system = reader["sys"];

        if (system != null)
        {
            _ = bool.TryParse(system, out isSystem);
        }

        var priv = reader["pri"];

        if (priv != null)
        {
            _ = bool.TryParse(priv, out isPrivate);
        }

        var sender = reader["sen"] ?? "";
        var receiver = reader["rec"];

        var text = reader.ReadElementContentAsString();

        return new Message(text ?? "", sender, receiver, isSystem, isPrivate);
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("M");

        if (!IsSystem)
        {
            writer.WriteAttributeString("sys", IsSystem.ToString());
        }

        if (IsPrivate)
        {
            writer.WriteAttributeString("pri", IsPrivate.ToString());
        }

        if (Sender != null && Sender.Length != 0)
        {
            writer.WriteAttributeString("sen", Sender);
        }

        if (Receiver != null && Receiver.Length != 0)
        {
            writer.WriteAttributeString("rec", Receiver);
        }

        writer.WriteString(Text);

        writer.WriteEndElement();
    }

    public byte[] Serialize()
    {
        using var memory = new MemoryStream();

        using (var writer = XmlWriter.Create(memory, Settings))
        {
            WriteXml(writer);
        }

        return memory.ToArray();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Message message)
        {
            return message.IsPrivate == IsPrivate &&
                message.IsSystem == IsSystem &&
                message.Sender == Sender &&
                message.Receiver == Receiver &&
                message.Text == Text;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode() => System.HashCode.Combine(IsPrivate, IsSystem, Text, Sender, Receiver);

    public static bool operator ==(Message left, Message right) => left.Equals(right);

    public static bool operator !=(Message left, Message right) => !(left == right);
}
