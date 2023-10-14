namespace SIPackages.Providers;

/// <summary>
/// Defines a package not found exception.
/// </summary>
public sealed class PackageNotFoundException : Exception
{
    public PackageNotFoundException()
    {
    }

    public PackageNotFoundException(string message)
        : base(message)
    {

    }

    public PackageNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
