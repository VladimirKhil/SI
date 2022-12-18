using SIPackages;
using SIQuester.ViewModel.Helpers;

namespace SIQuester.ViewModel;

public sealed class InfoViewModel : ModelViewBase
{
    private readonly Info _model;

    public AuthorsViewModel Authors { get; private set; }

    public SourcesViewModel Sources { get; private set; }

    public Comments Comments => _model.Comments;

    public IItemViewModel Owner { get; private set; }

    public InfoViewModel(Info model, IItemViewModel owner)
    {
        _model = model;
        Owner = owner;

        Authors = new AuthorsViewModel(_model.Authors, this);
        Sources = new SourcesViewModel(_model.Sources, this);

        BindHelper.Bind(Authors, _model.Authors);
        BindHelper.Bind(Sources, _model.Sources);
    }
}
