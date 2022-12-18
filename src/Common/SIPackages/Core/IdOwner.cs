using System.Runtime.Serialization;

namespace SIPackages.Core;

/// <summary>
/// Defines an object having an id.
/// </summary>
[DataContract]
[Serializable]
public abstract class IdOwner : PropertyChangedNotifier
{
    private string _id = Guid.NewGuid().ToString();

    /// <summary>
    /// Object id.
    /// </summary>
    [DataMember]
    public string Id
    {
        get => _id;
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
