namespace SICore.Special;

internal static class ResourceHelper
{
    internal static string GetSexString(string resource, bool isMale)
    {
        var resources = resource.Split(';');
        return resources[resources.Length == 1 || isMale ? 0 : 1];
    }
}
