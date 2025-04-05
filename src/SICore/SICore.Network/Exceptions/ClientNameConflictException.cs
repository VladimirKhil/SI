namespace SICore.Network.Exceptions;

public sealed class ClientNameConflictException : Exception
{
    public ClientNameConflictException(string message) : base(message)
    {
    }
    public ClientNameConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public ClientNameConflictException()
    {
    }
}
