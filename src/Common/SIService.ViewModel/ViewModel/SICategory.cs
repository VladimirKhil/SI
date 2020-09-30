using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Services.SI.ViewModel
{
    public sealed class SICategory : INotifyPropertyChanged
    {
        private readonly PackageCategory _category;
        private readonly SIStorageServiceClient _siService;

        private string _restriction;

        private Package[] _packages;

        public Package[] Packages
        {
            get
            {
                if (_packages == null && !_isLoading)
                    LoadPackagesAsync();

                return _packages;
            }
            set
            {
                _packages = value;
                OnPropertyChanged();
            }
        }

        private Package _currentPackage;

        public Package CurrentPackage
        {
            get { return _currentPackage; }
            set { if (_currentPackage != value) { _currentPackage = value; OnPropertyChanged(); } }
        }

        public string Name => _category.Name;

        private bool _isLoading;

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public event Action<Exception> Error;

        public SICategory(PackageCategory category, SIStorageServiceClient siService, string restriction)
        {
            _category = category;
            _restriction = restriction;
            _siService = siService;
        }

        private async void LoadPackagesAsync()
        {
            IsLoading = true;
            try
            {
                var packages = await _siService.GetPackagesByCategoryAndRestrictionAsync(_category.ID, _restriction);

                Packages = packages;
                if (_packages.Length > 0)
                    CurrentPackage = _packages[0];
            }
            catch (Exception exc)
            {
                Error?.Invoke(exc);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Update(string newRestriction)
        {
            _restriction = newRestriction;
            Packages = null;
        }
    }
}
