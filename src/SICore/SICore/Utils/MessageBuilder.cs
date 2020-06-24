using SICore.Connections;
using System.Collections.Generic;

namespace SICore.Utils
{
    public sealed class MessageBuilder
    {
        private readonly List<object> _messageArgs = new List<object>();

        public MessageBuilder()
        {

        }

        public MessageBuilder(object arg)
        {
            _messageArgs.Add(arg);
        }

        public MessageBuilder(params object[] args)
        {
            _messageArgs.AddRange(args);
        }

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
    }
}
