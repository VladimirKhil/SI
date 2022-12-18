using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

public sealed class HowToViewModel : WorkspaceViewModel
{
    public override string Header => Resources.HowToUseApp;

    private readonly IXpsDocumentWrapper _documentWrapper;

    public object Document => _documentWrapper.GetDocument();

    public HowToViewModel() => _documentWrapper = PlatformManager.Instance.GetHelp();

    protected override async Task Close_Executed(object? arg)
    {
        _documentWrapper.Dispose();

        await base.Close_Executed(arg);
    }
}
