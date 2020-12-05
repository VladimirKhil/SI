using SIPackages.Core;
using System;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public sealed class MediaItemViewModel : ModelViewBase, IMediaOwner
    {
        public string Type { get; }

        public Named Model { get; }

        private readonly Func<IMedia> _mediaGetter;

        private IMedia _media;

        private bool _isMediaLoading;

        public IMedia MediaSource
        {
            get
            {
                if (_media == null && !_isMediaLoading)
                    LoadMedia();

                return _media;
            }
            set
            {
                _media = value;
                OnPropertyChanged(nameof(MediaSource));
            }
        }

        private async void LoadMedia()
        {
            try
            {
                await LoadMediaAsync();
            }
            catch
            {

            }
        }

        public async Task<IMedia> LoadMediaAsync()
        {
            if (_media != null)
            {
                return _media;
            }

            _isMediaLoading = true;
            try
            {
                return await Task.Run(() =>
                {
                    _media = _mediaGetter();
                    OnPropertyChanged(nameof(MediaSource));

                    return _media;
                });
            }
            catch (Exception exc)
            {
                MainViewModel.ShowError(exc);
                return null;
            }
            finally
            {
                _isMediaLoading = false;
            }
        }

        public MediaItemViewModel(Named named, string type, Func<IMedia> mediaGetter)
        {
            Model = named;
            Type = type;
            _mediaGetter = mediaGetter;
        }
    }
}
