using System.IO;
using System.Threading.Tasks;

namespace SIGame.ViewModel.PackageSources
{
    /// <summary>
    /// Источник игрового пакета
    /// Варианты: следующий, случайный набор тем, из списка загруженных, подгрузить с диска
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
        public abstract Task<(string, bool)> GetPackageFileAsync();
        /// <summary>
        /// Получить пакет в виде набора байт
        /// </summary>
        /// <returns></returns>
        public virtual Task<Stream> GetPackageDataAsync() => null;
        /// <summary>
        /// Получить имя игрового пакета
        /// </summary>
        /// <returns></returns>
        public abstract string GetPackageName();
        /// <summary>
        /// Получить идентификатор пакета
        /// </summary>
        /// <returns></returns>
        public virtual string GetPackageId() => null;
        /// <summary>
        /// Получить уникальный хэш игрового пакета
        /// </summary>
        /// <returns></returns>
        public abstract Task<byte[]> GetPackageHashAsync();

        public virtual bool RandomSpecials => false;

        public override string ToString() => Source;
    }
}
