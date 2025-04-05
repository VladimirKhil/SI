namespace SICore.Network.Exceptions;

public sealed class InvalidReceiverMessageException : Exception
{
    public InvalidReceiverMessageException(string message) : base(message)
    {
    }
    public InvalidReceiverMessageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
