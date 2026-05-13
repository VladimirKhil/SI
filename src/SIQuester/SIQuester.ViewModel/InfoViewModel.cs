using SIPackages;
using SIQuester.ViewModel.Helpers;

namespace SIQuester.ViewModel;

public sealed class InfoViewModel : ModelViewBase
{
    public AuthorsViewModel Authors { get; }

    public SourcesViewModel Sources { get; }

    public CommentsViewModel Comments { get; }

    public ShowmanCommentsViewModel ShowmanComments { get; }

    public IItemViewModel Owner { get; }

    public InfoViewModel(Info model, IItemViewModel owner)
    {
        Owner = owner;

        Authors = new AuthorsViewModel(model.Authors, this);
        Sources = new SourcesViewModel(model.Sources, this);
        Comments = new CommentsViewModel(model.Comments);
        ShowmanComments = new ShowmanCommentsViewModel(model);

        BindHelper.Bind(Authors, model.Authors);
        BindHelper.Bind(Sources, model.Sources);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ShowmanComments.Dispose();
        }

        base.Dispose(disposing);
    }
}
