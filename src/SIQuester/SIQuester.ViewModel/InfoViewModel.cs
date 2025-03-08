using SIPackages;
using SIQuester.ViewModel.Helpers;

namespace SIQuester.ViewModel;

public sealed class InfoViewModel : ModelViewBase
{
    private readonly Info _model;

    public AuthorsViewModel Authors { get; }

    public SourcesViewModel Sources { get; }

    public CommentsViewModel Comments { get; }

    public IItemViewModel Owner { get; }

    public InfoViewModel(Info model, IItemViewModel owner)
    {
        _model = model;
        Owner = owner;

        Authors = new AuthorsViewModel(_model.Authors, this);
        Sources = new SourcesViewModel(_model.Sources, this);
        Comments = new CommentsViewModel(_model.Comments);

        BindHelper.Bind(Authors, _model.Authors);
        BindHelper.Bind(Sources, _model.Sources);
    }
}
