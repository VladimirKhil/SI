using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SIQuester.Behaviors;

/// <summary>
/// Office-like smooth caret for all text boxes: the native caret is hidden and an animated one
/// glides to the new position and blinks softly.
/// </summary>
internal static class SmoothCaret
{
    private static TextBox? _textBox;
    private static CaretAdorner? _adorner;
    private static Brush? _originalCaretBrush;

    /// <summary>
    /// Enables the smooth caret for every TextBox in the application.
    /// </summary>
    public static void Register()
    {
        EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotFocus), true);
        EventManager.RegisterClassHandler(typeof(TextBox), UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostFocus), true);
    }

    private static void OnGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox || e.NewFocus != textBox || textBox.IsReadOnly)
        {
            return;
        }

        Detach();

        var layer = AdornerLayer.GetAdornerLayer(textBox);

        if (layer == null)
        {
            return; // keep the native caret
        }

        _textBox = textBox;
        _originalCaretBrush = textBox.CaretBrush;
        _adorner = new CaretAdorner(textBox);
        layer.Add(_adorner);
        textBox.CaretBrush = Brushes.Transparent;

        textBox.SelectionChanged += Update;
        textBox.TextChanged += Update;
        textBox.SizeChanged += Snap;
        textBox.LayoutUpdated += OnLayoutUpdated;

        // The text box may still be sliding in (side panel tabs, expanding themes): keep the caret hidden
        // until it stops moving, then put the caret in place without animation
        _adorner.Opacity = 0;
        _settleFrames = 0;
        _lastOrigin = null;
        StartWatching();
    }

    private static int _settleFrames;
    private static int _stillFrames;
    private static bool _watching;
    private static Point? _lastOrigin;

    /// <summary>
    /// Frames the box must stay still (with the caret shown) before watching stops: covers a slide
    /// that starts a little after the focus. Watching every frame keeps WPF rendering, so it is not left on.
    /// </summary>
    private const int StillFramesToStop = 45;

    private static void StartWatching()
    {
        _stillFrames = 0;

        if (!_watching)
        {
            _watching = true;
            CompositionTarget.Rendering += WaitUntilSettled;
        }
    }

    private static void StopWatching()
    {
        if (_watching)
        {
            _watching = false;
            CompositionTarget.Rendering -= WaitUntilSettled;
        }
    }

    private static void WaitUntilSettled(object? sender, EventArgs e)
    {
        var textBox = _textBox;
        var adorner = _adorner;

        if (textBox == null || adorner == null)
        {
            StopWatching();
            return;
        }

        Point? origin = null;

        try
        {
            if (textBox.IsVisible && AdornerLayer.GetAdornerLayer(textBox) is AdornerLayer layer)
            {
                origin = textBox.TransformToVisual(layer).Transform(new Point(0, 0));
            }
        }
        catch
        {
            // Not connected yet
        }

        var moved = !(origin.HasValue && _lastOrigin.HasValue && (origin.Value - _lastOrigin.Value).Length < 0.5);
        _lastOrigin = origin;

        if (moved)
        {
            // The box is sliding (tab switch, expanding theme, scrolling): hide the caret until it stops,
            // the adorner layer does not follow such moves by itself
            _settleFrames = 0;
            _stillFrames = 0;
            adorner.Opacity = 0;
            return;
        }

        if (adorner.Opacity > 0)
        {
            // Standing still and already shown: stop watching after a while
            if (++_stillFrames >= StillFramesToStop)
            {
                StopWatching();
            }

            return;
        }

        if (++_settleFrames < 4)
        {
            return;
        }

        AdornerLayer.GetAdornerLayer(textBox)?.Update(textBox);
        adorner.InvalidateArrange();
        adorner.MoveTo(textBox, animate: false);
        adorner.Opacity = 1;
    }

    private static void Snap(object? sender, EventArgs e)
    {
        if (_textBox != null)
        {
            _adorner?.MoveTo(_textBox, animate: false);
        }
    }

    private static void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender == _textBox && e.OldFocus == _textBox)
        {
            Detach();
        }
    }

    private static void Update(object? sender, EventArgs e)
    {
        if (_textBox != null)
        {
            _adorner?.MoveTo(_textBox, animate: true);
        }
    }

    // Scrolling or relayout can move the text without changing the caret index; the box itself
    // may start moving too (scrolling, expanding), so watch it again for a short while
    private static void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_textBox != null)
        {
            _adorner?.MoveTo(_textBox, animate: false, onlyIfMoved: true);
            StartWatching();
        }
    }

    private static void Detach()
    {
        if (_textBox == null)
        {
            return;
        }

        _textBox.SelectionChanged -= Update;
        _textBox.TextChanged -= Update;
        _textBox.SizeChanged -= Snap;
        _textBox.LayoutUpdated -= OnLayoutUpdated;
        StopWatching();

        if (ReferenceEquals(_textBox.CaretBrush, Brushes.Transparent))
        {
            if (_originalCaretBrush == null)
            {
                _textBox.ClearValue(System.Windows.Controls.Primitives.TextBoxBase.CaretBrushProperty);
            }
            else
            {
                _textBox.CaretBrush = _originalCaretBrush;
            }
        }

        if (_adorner != null)
        {
            AdornerLayer.GetAdornerLayer(_textBox)?.Remove(_adorner);
        }

        _textBox = null;
        _adorner = null;
        _originalCaretBrush = null;
    }

    private sealed class CaretAdorner : Adorner
    {
        private static readonly Duration MoveDuration = new(TimeSpan.FromMilliseconds(80));

        private readonly Rectangle _caret;
        private readonly TranslateTransform _position = new();
        private Rect _lastRect = Rect.Empty;

        public CaretAdorner(TextBox textBox) : base(textBox)
        {
            IsHitTestVisible = false;

            _caret = new Rectangle
            {
                Width = 1.5,
                Height = 0,
                Fill = textBox.Foreground is SolidColorBrush brush ? brush : Brushes.Black,
                RenderTransform = _position,
                IsHitTestVisible = false,
            };

            AddVisualChild(_caret);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _caret;

        protected override Size MeasureOverride(Size constraint)
        {
            _caret.Measure(constraint);
            return AdornedElement.RenderSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var height = double.IsFinite(_caret.Height) && _caret.Height > 0 ? _caret.Height : 0;
            _caret.Arrange(new Rect(0, 0, _caret.Width, height));
            return finalSize;
        }

        public void MoveTo(TextBox textBox, bool animate, bool onlyIfMoved = false)
        {
            Rect rect;

            try
            {
                rect = textBox.GetRectFromCharacterIndex(Math.Min(textBox.CaretIndex, textBox.Text.Length));
            }
            catch
            {
                return;
            }

            if (rect.IsEmpty || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y) || !double.IsFinite(rect.Height))
            {
                return;
            }

            if (onlyIfMoved && rect == _lastRect)
            {
                return;
            }

            _lastRect = rect;

            if (Math.Abs(_caret.Height - rect.Height) > 0.1)
            {
                _caret.Height = rect.Height;
                InvalidateArrange();
            }

            // Long jumps (another line far away, a click elsewhere) are not animated: only short glides look smooth
            if (animate && (Math.Abs(rect.X - _position.X) > 160 || Math.Abs(rect.Y - _position.Y) > rect.Height * 1.5))
            {
                animate = false;
            }

            if (animate)
            {
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                _position.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(rect.X, MoveDuration) { EasingFunction = ease });
                _position.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(rect.Y, MoveDuration) { EasingFunction = ease });
            }
            else
            {
                _position.BeginAnimation(TranslateTransform.XProperty, null);
                _position.BeginAnimation(TranslateTransform.YProperty, null);
                _position.X = rect.X;
                _position.Y = rect.Y;
            }

            RestartBlink();
        }

        // Caret stays solid while moving/typing, then blinks softly
        private void RestartBlink()
        {
            // Like the Windows caret: blinks for a while, then stays on (an endless animation would keep WPF rendering non-stop)
            var blink = new DoubleAnimationUsingKeyFrames { RepeatBehavior = new RepeatBehavior(10) };
            blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            blink.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));
            blink.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(650))));
            blink.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(950))));
            blink.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1100))));
            _caret.BeginAnimation(OpacityProperty, blink);
        }
    }
}
