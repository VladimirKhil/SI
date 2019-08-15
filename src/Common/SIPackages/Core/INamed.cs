using System.ComponentModel;

namespace SIPackages.Core
{
    /// <summary>
    /// Именованный объект
    /// </summary>
    public interface INamed : INotifyPropertyChanged
    {
        /// <summary>
        /// Имя объекта
        /// </summary>
        string Name { get; set; }
    }
}
