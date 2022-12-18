namespace SIQuester.Model;

[AttributeUsage(AttributeTargets.Field)]
public sealed class PackageTypeNameAttribute : Attribute
{
    public string Name { get; set; }

    internal PackageTypeNameAttribute(string name) => Name = name;
}
