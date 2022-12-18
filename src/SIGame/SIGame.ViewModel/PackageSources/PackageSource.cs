namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Provides game package source.
/// </summary>
public abstract class PackageSource
{
    /// <summary>
    /// Уникальный код источника
    /// </summary>
    public abstract PackageSourceKey Key { get; }

    /// <summary>
    /// Описание источника
    /// </summary>
    public abstract string Source { get; }

    /// <summary>
    /// Получить игровой пакет
    /// </summary>
    public abstract Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package contents as stream.
    /// </summary>
    public virtual Task<Stream> GetPackageDataAsync(CancellationToken cancellationToken = default) => null;

    /// <summary>
    /// Получить имя игрового пакета
    /// </summary>
    /// <returns></returns>
    public abstract string GetPackageName();

    /// <summary>
    /// Получить идентификатор пакета
    /// </summary>
    public virtual string GetPackageId() => null;

    /// <summary>
    /// Получить уникальный хэш игрового пакета
    /// </summary>
    public abstract Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default);

    public virtual bool RandomSpecials => false;

    public override string ToString() => Source;
}
