using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace SIUI.ViewModel
{
    /// <summary>
    /// Информация об игроке для табло
    /// </summary>
    [DataContract]
    public class SimplePlayerInfo : INotifyPropertyChanged
    {
        private string _name = null;

        /// <summary>
        /// Имя игрока
        /// </summary>
        [DataMember]
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private int _sum = 0;

        /// <summary>
        /// Счёт игрока
        /// </summary>
        [DataMember]
        public int Sum
        {
            get => _sum;
            set { if (_sum != value) { _sum = value; OnPropertyChanged(); } }
        }

        public override string ToString() => $"{_name}: {_sum}";

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Члены INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
