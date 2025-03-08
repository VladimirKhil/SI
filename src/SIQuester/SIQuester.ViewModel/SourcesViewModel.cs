using SIPackages;
using SIQuester.ViewModel.PlatformSpecific;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class SourcesViewModel : LinksViewModel
{
    public Sources Model { get; private set; }

    public string? Url
    {
        get
        {
            foreach (var source in this)
            {
                var url = TryGetUrl(source);

                if (url != null)
                {
                    return url;
                }
            }

            return null;
        }
    }

    public ICommand OpenUri { get; }

    public SourcesViewModel(Sources model, InfoViewModel owner)
        : base(model, owner)
    {
        Model = model;
        OpenUri = new SimpleCommand(OpenUri_Executed);
    }

    private void OpenUri_Executed(object? arg)
    {
        if (Url == null)
        {
            return;
        }

        try
        {
            Browser.Open(Url);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
        }
    }

    protected override void LinkTo(int index, object arg)
    {
        var document = OwnerDocument ?? throw new InvalidOperationException("Document is undefined");
        document.Document.SetSourceLink(this, index, document.Document.Sources.IndexOf((SourceInfo)arg));
    }

    private static string? TryGetUrl(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return uriResult.ToString();
        }

        return null;
    }
}
