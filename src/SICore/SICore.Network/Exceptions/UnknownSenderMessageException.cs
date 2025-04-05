namespace SICore.Network.Exceptions;

public sealed class UnknownSenderMessageException : Exception
{
    public UnknownSenderMessageException(string message) : base(message)
    {
    }
    public UnknownSenderMessageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
