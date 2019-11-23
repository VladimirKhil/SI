using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SIQuester.Model
{
    /// <summary>
    /// Тип пакета
    /// </summary>
    public enum PackageType
    {
        [PackageTypeName(@"Стандартный пакет для SIGame")]
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
    /// Формат экспорта
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

    public enum ViewMode
    {
        [Description("Дерево")]
        TreeFull,
        [Description("Плитки")]
        Flat
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
