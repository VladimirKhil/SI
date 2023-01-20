using SIData;

namespace SICore.Utils;

// TODO: rewrite without boxing (use underlying StringBuilder)

/// <summary>
/// Allows to build a well-formed SIGame message text.
/// </summary>
public sealed class MessageBuilder
{
    private readonly List<object> _messageArgs = new();

    public MessageBuilder() { }

    public MessageBuilder(object arg) => _messageArgs.Add(arg);

    public MessageBuilder(params object[] args) => _messageArgs.AddRange(args);

    public MessageBuilder Add(object arg)
    {
        _messageArgs.Add(arg);
        return this;
    }

    public MessageBuilder AddRange(IEnumerable<object> args)
    {
        _messageArgs.AddRange(args);
        return this;
    }

    public string Build() => string.Join(Message.ArgsSeparator, _messageArgs);

    public override string ToString() => Build();
}
