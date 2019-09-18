using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Services.SI.ViewModel
{
    [Obsolete]
    public sealed class SIStorage : INotifyPropertyChanged
    {
        private SIStorageService _siService;

        /// <summary>
        /// Выбранный пакет
        /// </summary>
        public Package SelectedPackage
        {
            get
            {
                if (_categories == null)
                    return null;

                SICategory category = _currentCategory;
                if (category == null || category.Packages == null)
                    return null;

                return category.CurrentPackage;
            }
        }

        private SICategory[] _categories;

        private SICategory _currentCategory;

        public SICategory CurrentCategory
        {
            get { return _currentCategory; }
            set { if (_currentCategory != value) { _currentCategory = value; OnPropertyChanged(); } }
        }
        
        private bool _isLoading;

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }
        
        public SICategory[] Categories
        {
            get
            {
                if (_categories == null && !_isLoading)
                    LoadCategoriesAsync();

                return _categories;
            }
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public string[] Restrictions { get; } = new string[] { "18+", "12+", " " };

        private string _restriction;

        public string Restriction
        {
            get { return _restriction; }
            set
            {
                if (_restriction != value)
                {
					if (value == "(не исключать)")
						value = " ";

                    _restriction = value;
                    OnPropertyChanged();

                    if (_categories != null)
                    {
                        foreach (var item in _categories)
                        {
                            item.Update(value);
                        }
                    }
                }
            }
        }

        public event Action<Exception> Error;

        public SIStorage()
        {
            _restriction = "";
        }

        public SIStorage(string restriction)
        {
            _restriction = restriction;
        }

        public void Open()
        {
            _siService = new SIStorageService();
        }

        public async Task<Uri> LoadSelectedPackageUriAsync()
        {
            if (_siService == null || SelectedPackage == null)
                return null;

            return await _siService.GetPackageByIDAsync(SelectedPackage.ID);
        }

        private async void LoadCategoriesAsync()
        {
            IsLoading = true;
            try
            {
                PackageCategory[] categories = await _siService.GetCategoriesAsync();
                Categories = categories.Select(pc => new SICategory(pc, _siService, Restriction)).ToArray();

                if (_categories.Length > 0)
                    CurrentCategory = _categories[0];

                foreach (var category in _categories)
                {
                    category.Error += OnError;
                }
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnError(Exception exc)
        {
            Error?.Invoke(exc);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
