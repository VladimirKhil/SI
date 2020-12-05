using SIPackages;
using SIPackages.Core;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
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
                root = root.Owner;

            if (!(root is PackageViewModel packageViewModel))
            {
                return null;
            }

            _mediaSource = packageViewModel.Document.Wrap(this);

            return _mediaSource;
        }

        public Task<IMedia> LoadMediaAsync()
        {
            return Task.FromResult(LoadMedia());
        }

        private bool _isExpanded = true;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public AtomViewModel(Atom model)
        {
            Model = model;
        }
    }
}
