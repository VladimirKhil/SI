using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel;

/// <summary>
/// Базовая модель отображения
/// </summary>
/// <typeparam name="TModel">Тип модели</typeparam>
public abstract class ViewModel<TModel> : INotifyPropertyChanged
    where TModel: new()
{
    protected TModel _model;

    /// <summary>
    /// Соответствующая модель
    /// </summary>
    public TModel Model => _model;

    protected ViewModel()
    {
        _model = new TModel();
        Initialize();
    }

    protected ViewModel(TModel model)
    {
        _model = model;
        Initialize();
    }

    protected virtual void Initialize()
    {
        
    }

    /// <summary>
    /// Изменилось значение свойства
    /// </summary>
    /// <param name="name">Имя свойства</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
