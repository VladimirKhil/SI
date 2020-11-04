using Services.SI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel
{
    public sealed class StorageContextViewModel : INotifyPropertyChanged
    {
        private readonly SIStorageServiceClient _siStorageService;

        private string[] _publishers;

        public string[] Publishers
        {
            get
            {
                return _publishers;
            }
            set
            {
                _publishers = value;
                OnPropertyChanged();
            }
        }

        private string[] _authors;

        public string[] Authors
        {
            get
            {
                return _authors;
            }
            set
            {
                _authors = value;
                OnPropertyChanged();
            }
        }

        private string[] _tags;

        public string[] Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public string[] Languages { get; } = new string[] { "ru-RU", "en-US" };

        public StorageContextViewModel(SIStorageServiceClient siStorageService)
        {
            _siStorageService = siStorageService;
        }

        public async void Load()
        {
            try
            {
                Publishers = (await _siStorageService.GetPublishersAsync()).Select(named => named.Name).ToArray();
                Tags = (await _siStorageService.GetTagsAsync()).Select(named => named.Name).ToArray();
            }
            catch (Exception exc)
            {
                Trace.TraceWarning(exc.ToString());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
