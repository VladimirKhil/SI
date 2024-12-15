namespace SIPackages.Helpers;

internal static class InfoOwnerExtensions
{
    /// <summary>
    /// Copies info from source object.
    /// </summary>
    /// <param name="target">Target object.</param>
    /// <param name="infoOwner">Source object.</param>
    internal static void SetInfoFromOwner(this InfoOwner target, InfoOwner infoOwner)
    {
        foreach (string s in infoOwner.Info.Authors)
        {
            target.Info.Authors.Add(s);
        }

        foreach (string s in infoOwner.Info.Sources)
        {
            target.Info.Sources.Add(s);
        }

        target.Info.Comments.Text = infoOwner.Info.Comments.Text;

        if (infoOwner.Info.ShowmanComments != null)
        {
            target.Info.ShowmanComments ??= new Comments();
            target.Info.ShowmanComments.Text = infoOwner.Info.ShowmanComments.Text;
        }

        target.Info.Extension = infoOwner.Info.Extension;
    }
}
