using System;
using System.Runtime.Serialization;

namespace SIPackages.Core
{
    /// <summary>
    /// Владелец идентификатора
    /// </summary>
    [DataContract]
    public abstract class IdOwner : PropertyChangedNotifier
    {
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Идентификатор
        /// </summary>
        [DataMember]
        public string Id
        {
            get { return _id; }
            set
            {
                var oldValue = _id;
                if (oldValue != value)
                {
                    _id = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }
    }
}
