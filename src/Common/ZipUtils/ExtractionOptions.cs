namespace ZipUtils;

/// <summary>
/// Provides zip file extraction options.
/// </summary>
public sealed class ExtractionOptions
{
    /// <summary>
    /// Default options.
    /// </summary>
    public static readonly ExtractionOptions Default = new();

    /// <summary>
    /// Default maximum archive file size in bytes.
    /// </summary>
    public const int DefaultMaxArchiveDataLength = 2 * 100 * 1024 * 1024;

    /// <summary>
    /// Maximum allowed length of extracted data in the archive.
    /// <see cref="DefaultMaxArchiveDataLength" /> by default.
    /// </summary>
    public long MaxAllowedDataLength { get; set; } = DefaultMaxArchiveDataLength;

    /// <summary>
    /// Optional filter for archive files.
    /// </summary>
    public Predicate<string>? FileFilter { get; set; } = null;

    /// <summary>
    /// Optional selector for file naming mode. When it returns null, efault naming mode is used.
    /// </summary>
    public Func<string, UnzipNamingMode> FileNamingModeSelector { get; } = (name) => UnzipNamingMode.KeepOriginal;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionOptions" /> class.
    /// </summary>
    public ExtractionOptions() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionOptions" /> class.
    /// </summary>
    /// <param name="namingMode">Default file naming mode.</param>
    public ExtractionOptions(UnzipNamingMode namingMode) => FileNamingModeSelector = (name) => namingMode;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionOptions" /> class.
    /// </summary>
    /// <param name="namingModeSelector">File naming mode selector.</param>
    public ExtractionOptions(Func<string, UnzipNamingMode> namingModeSelector) => FileNamingModeSelector = namingModeSelector;
}
