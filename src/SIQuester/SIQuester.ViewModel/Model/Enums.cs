using System.ComponentModel;

namespace SIQuester.Model
{
    /// <summary>
    /// Defines well-known package templates.
    /// </summary>
    public enum PackageType
    {
        [PackageTypeName("Стандартный пакет для SIGame")]
        [Description("Такой же пакет, как и в Телевизионном аналоге SIGame")]
        [Category("SIGame")]
        Classic,
        [PackageTypeName("Нестандартный пакет")]
        [Description("Пакет, содержащий какое-то иное число раундов, тем или вопросов, чем в стандартной SIGame")]
        [Category("SIGame")]
        Special,
        [PackageTypeName("Коллекция тем")]
        [Description("Просто набор тем")]
        [Category("SIGame")]
        ThemesCollection,
        [PackageTypeName("Пустой пакет")]
        [Description("Пакет, изначально ничего не содержащий")]
        [Category("Общее")]
        Empty
    }

    /// <summary>
    /// Defines well-known packge export formats.
    /// </summary>
    public enum ExportFormats
    {
        [Description("Динабанк")]
        Dinabank,
        [Description("Телевизионный аналог SIGame")]
        TvSI,
        [Description("СНС")]
        Sns,
        [Description("База вопросов")]
        Db
    }

    public enum Orientation
    {
        [Description("в столбец")]
        Vertical,
        [Description("в строку")]
        Horizontal
    }

    /// <summary>
    /// Defines document view modes.
    /// </summary>
    public enum ViewMode
    {
        /// <summary>
        /// Tree view mode.
        /// </summary>
        [Description("Дерево")]
        TreeFull,
        /// <summary>
        /// Flat view mode.
        /// </summary>
        [Description("Плитки")]
        Flat
    }

    /// <summary>
    /// Defines document layout modes in the flat view.
    /// </summary>
    public enum FlatLayoutMode
    {
        /// <summary>
        /// Table layout.
        /// </summary>
        [Description("Таблица")]
        Table,
        /// <summary>
        /// List layout.
        /// </summary>
        [Description("Список")]
        List
    }

    /// <summary>
    /// Режим редактирования
    /// </summary>
    public enum EditMode
    {
        [Description("Только чтение")]
        None,
        [Description("Фиксированная панель")]
        FixedPanel,
        [Description("Плавающая панель")]
        FloatPanel
    }

    /// <summary>
    /// Масштаб плиточного представления
    /// </summary>
    public enum FlatScale
    {
        [Description("Пакет")]
        Package,
        [Description("Раунд")]
        Round,
        [Description("Тема")]
        Theme,
        [Description("Вопрос")]
        Question
    }
}
