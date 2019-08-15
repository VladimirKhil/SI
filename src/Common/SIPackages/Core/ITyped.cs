using System.ComponentModel;

namespace SIPackages.Core
{
    /// <summary>
    /// Типизированный объект
    /// </summary>
    public interface ITyped : INotifyPropertyChanged
    {
        /// <summary>
        /// Тип объекта
        /// </summary>
        string Type { get; set; }
    }
}
