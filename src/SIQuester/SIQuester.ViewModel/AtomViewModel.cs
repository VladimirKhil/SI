using SIPackages;
using SIPackages.Core;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Represents an question scenario atom view model.
    /// </summary>
    public sealed class AtomViewModel : ModelViewBase, IMediaOwner
    {
        public Atom Model { get; }

        public ScenarioViewModel OwnerScenario { get; set; }

        private IMedia _mediaSource = null;

        public IMedia MediaSource
        {
            get
            {
                if (_mediaSource == null)
                {
                    LoadMedia();
                }

                return _mediaSource;
            }
            set
            {
                if (_mediaSource != value)
                {
                    _mediaSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public IMedia LoadMedia()
        {
            if (_mediaSource != null)
            {
                return _mediaSource;
            }

            IItemViewModel root = OwnerScenario.Owner;

            while (root.Owner != null)
            {
                root = root.Owner;
            }

            if (root is not PackageViewModel packageViewModel)
            {
                return null;
            }

            _mediaSource = packageViewModel.Document.Wrap(Model);

            return _mediaSource;
        }

        public Task<IMedia> LoadMediaAsync() => Task.FromResult(LoadMedia());

        private bool _isExpanded = true;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public AtomViewModel(Atom model) => Model = model;
    }
}
