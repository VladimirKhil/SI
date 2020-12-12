using System.IO;
using System.Xml;

namespace SICore.Connections
{
    /// <summary>
    /// Сообщение
    /// </summary>
    public struct Message
    {
        /// <summary>
        /// Разделитель параметров в сообщении
        /// </summary>
        public const char ArgsSeparatorChar = '\n';

        /// <summary>
        /// Разделитель параметров в сообщении
        /// </summary>
        public const string ArgsSeparator = "\n";

        private static readonly XmlWriterSettings Settings = new XmlWriterSettings();

        internal static Message Empty = new Message();

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Является ли системным
        /// </summary>
        public bool IsSystem { get; private set; }

        /// <summary>
        /// Приватное ли?
        /// </summary>
        public bool IsPrivate { get; private set; }

        /// <summary>
        /// Отправитель
        /// </summary>
        public string Sender { get; private set; }

        /// <summary>
        /// Получатель
        /// </summary>
        public string Receiver { get; private set; }

        static Message()
        {
            Settings.OmitXmlDeclaration = true;
            Settings.Encoding = System.Text.Encoding.UTF8;
        }

        public Message(string text, string sender, string receiver = null, bool isSystem = true, bool isPrivate = false)
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
                bool.TryParse(system, out isSystem);

            var priv = reader["pri"];
            if (priv != null)
                bool.TryParse(priv, out isPrivate);

            var sender = reader["sen"];
            var receiver = reader["rec"];

            var text = reader.ReadElementContentAsString();

            return new Message(text ?? "", sender, receiver, isSystem, isPrivate);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("M");
            if (!IsSystem)
                writer.WriteAttributeString("sys", IsSystem.ToString());

            if (IsPrivate)
                writer.WriteAttributeString("pri", IsPrivate.ToString());

            if (Sender != null && Sender.Length != 0)
                writer.WriteAttributeString("sen", Sender);

            if (Receiver != null && Receiver.Length != 0)
                writer.WriteAttributeString("rec", Receiver);

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

        public override bool Equals(object obj)
        {
            if (obj is Message message)
            {
                return message.IsPrivate == IsPrivate && message.IsSystem == IsSystem
                    && message.Sender == Sender && message.Receiver == Receiver && message.Text == Text;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            var hash = Text.GetHashCode();
            hash = hash * 31 + Sender.GetHashCode();
            hash = hash * 31 + Receiver.GetHashCode();

            return hash;
        }

        public static bool operator ==(Message left, Message right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Message left, Message right)
        {
            return !(left == right);
        }
    }
}
