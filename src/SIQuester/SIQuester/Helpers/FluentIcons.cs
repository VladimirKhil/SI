using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SIQuester.Helpers;

/// <summary>
/// Replaces the classic toolbar images with monochrome Windows 11 / Office-style glyphs
/// from the "Segoe Fluent Icons" font (falls back to "Segoe MDL2 Assets" on Windows 10).
/// </summary>
internal static class FluentIcons
{
    private static readonly Color Neutral = Color.FromRgb(0x42, 0x42, 0x42);
    private static readonly Color Accent = Color.FromRgb(0x0F, 0x6C, 0xBD);
    private static readonly Color Danger = Color.FromRgb(0xC4, 0x2B, 0x1C);
    private static readonly Color Success = Color.FromRgb(0x10, 0x7C, 0x10);

    // Resource key -> (glyph, color)
    private static readonly (string Key, string Glyph, Color Color)[] Map =
    [
        ("AddImage", "", Accent),              // Add
        ("RemoveImage", "", Danger),           // Delete
        ("CloneImage", "", Neutral),           // Copy
        ("PasteImage", "", Neutral),           // Paste
        ("TextImage", "", Neutral),            // Font
        ("AddEmptyImage", "", Neutral),        // Page
        ("QuestionTypeImage", "", Neutral),    // List (question type)
        ("AnswerTypeImage", "", Success),      // CheckMark (answer type)
        ("AddMarkerImage", "", Neutral),       // Checkbox (add complex answer)
        ("RemoveMarkerImage", "", Neutral),    // CheckboxComposite (remove complex answer)
        ("WrongAnswersImage", "", Danger),     // Cancel (wrong answers)
        ("AddTimeImage", "", Neutral),         // Stopwatch
        ("PlayImage", "", Success),            // Play
        ("AuthorsImageKey", "", Neutral),      // Contact
        ("SourcesImageKey", "", Neutral),      // ReadingMode (book)
        ("SumImage", "", Neutral),             // Comment
        ("ShowmanCommentsImage", "", Neutral), // Message
        ("AddImageImage", "", Neutral),        // Picture
        ("AddAudioImage", "", Neutral),        // MusicNote
        ("AddVideoImage", "", Neutral),        // Video
        ("AddHtmlImage", "", Neutral),         // Globe
        ("AddVoiceImage", "", Neutral),        // Microphone (oral text)
        ("ChangeTypeImage", "", Neutral),      // Switch
        ("JoinWithNextImage", "", Neutral),    // Link
        ("JoinWithNextActiveImage", "", Accent),
        ("CollapseMediaImage", "", Neutral),   // BackToWindow
        ("ExpandMediaImage", "", Neutral),     // FullScreen
        ("ExportMediaImage", "", Neutral),     // Save
        ("LeftImageKey", "", Neutral),         // Back
        ("RightImageKey", "", Neutral),        // Forward
        ("RightImageGrayKey", "", Neutral),
        ("UpImage", "", Neutral),              // Up
        ("DownImage", "", Neutral),            // Down
        ("SortImage", "", Neutral),            // Sort
        ("ShuffleImage", "", Neutral),         // Shuffle
        ("GenerateQuestionsImage", "", Accent),// Lightbulb
        ("Selection", "", Neutral),            // More (…)
    ];

    /// <summary>
    /// Font family used for icon glyphs, or null when no icon font is installed.
    /// </summary>
    public static FontFamily? IconFont { get; private set; }

    public static void Apply(ResourceDictionary resources, bool isDark = false)
    {
        IconFont = FindIconFont();

        if (IconFont == null)
        {
            return; // keep the classic images
        }

        var typeface = new Typeface(IconFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        foreach (var (key, glyph, color) in Map)
        {
            try
            {
                // Dark theme: light gray neutral icons and a brighter accent
                var themedColor = !isDark ? color : color == Neutral ? Color.FromRgb(0xDA, 0xDA, 0xDA) : color == Accent ? Color.FromRgb(0x4C, 0xA0, 0xE0) : color;
                var newImage = CreateImage(typeface, glyph, themedColor);

                // Templates already hold references to the original DrawingImage objects (resolved when App.xaml was parsed),
                // so the drawing is swapped inside the existing object when possible
                if (resources[key] is DrawingImage existing && !existing.IsFrozen)
                {
                    existing.Drawing = newImage.Drawing;
                }
                else
                {
                    resources[key] = newImage;
                }
            }
            catch
            {
                // Keep the original image for this key
            }
        }
    }

    private static FontFamily? FindIconFont()
    {
        foreach (var name in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
        {
            if (Fonts.SystemFontFamilies.Any(f => f.Source == name))
            {
                return new FontFamily(name);
            }
        }

        return null;
    }

    // Icons are rebuilt on every theme switch: keep the built ones (per glyph and color)
    private static readonly Dictionary<(string Glyph, Color Color), DrawingImage> Cache = new();

    private static DrawingImage CreateImage(Typeface typeface, string glyph, Color color)
    {
        if (Cache.TryGetValue((glyph, color), out var cached))
        {
            return cached;
        }

        var image = BuildImage(typeface, glyph, color);
        Cache[(glyph, color)] = image;
        return image;
    }

    private static DrawingImage BuildImage(Typeface typeface, string glyph, Color color)
    {
        const double size = 20;

        var text = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1.0);
        var geometry = text.BuildGeometry(new Point(0, 0));

        // Center the glyph in a size x size box (font line height would otherwise shift it down)
        var bounds = geometry.Bounds;
        var box = Math.Max(size, Math.Max(bounds.Width, bounds.Height));
        geometry.Transform = new TranslateTransform((box - bounds.Width) / 2 - bounds.X, (box - bounds.Height) / 2 - bounds.Y);

        var brush = new SolidColorBrush(color);
        brush.Freeze();

        // Transparent square keeps every icon in the same box so they line up
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, box, box))));
        group.Children.Add(new GeometryDrawing(brush, null, geometry));
        group.Freeze();

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
