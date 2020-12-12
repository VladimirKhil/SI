using System.Buffers;
using System.Text;

namespace SICore.Connections
{
    internal static class MessageSerializer
    {
        // Message buffer format:
        // [0]: IsSystem | IsPrivate
        // Sender
        // Separator
        // Receiver
        // Separator
        // Text
        // 0

        private const byte Separator = (byte)'\n';

        public static int GetBufferSizeForMessage(Message message) =>
            4 + Encoding.UTF8.GetByteCount(message.Sender)
            + Encoding.UTF8.GetByteCount(message.Receiver ?? "")
            + Encoding.UTF8.GetByteCount(message.Text);

        public static void SerializeMessage(Message message, byte[] buffer)
        {
            buffer[0] = (byte)((message.IsSystem ? 1 : 0) + (message.IsPrivate ? 2 : 0));

            var index = 1;
            index += Encoding.UTF8.GetBytes(message.Sender, 0, message.Sender.Length, buffer, index);
            buffer[index++] = Separator;
            var receiver = message.Receiver ?? "";
            index += Encoding.UTF8.GetBytes(receiver, 0, receiver.Length, buffer, index);
            buffer[index++] = Separator;
            index += Encoding.UTF8.GetBytes(message.Text, 0, message.Text.Length, buffer, index);
            buffer[index++] = 0;
        }

        internal static Message DeserializeMessage(ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 1 + 2 + 1)
            {
                return Message.Empty;
            }

            var data = buffer.Slice(0, 1).ToArray();

            var isSystem = (data[0] & 1) > 0;
            var isPrivate = (data[0] & 2) > 0;

            var position = buffer.PositionOf(Separator);
            if (!position.HasValue)
            {
                return Message.Empty;
            }

            var sender = Encoding.UTF8.GetString(buffer.Slice(1, position.Value).ToArray());
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            position = buffer.PositionOf(Separator);
            if (!position.HasValue)
            {
                return Message.Empty;
            }

            var receiver = Encoding.UTF8.GetString(buffer.Slice(0, position.Value).ToArray());
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            var text = Encoding.UTF8.GetString(buffer.ToArray());

            return new Message(text, sender, receiver, isSystem, isPrivate);
        }
    }
}
