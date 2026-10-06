using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Reverie.Gui.ViewModels;

namespace Reverie.Gui.Controls;

/// <summary>房间俯视图：可拖拽音箱，点击选中，坐标归一化 (0..1)。</summary>
public sealed class SpeakerLayoutCanvas : FrameworkElement
{
    public static readonly DependencyProperty SpeakersProperty =
        DependencyProperty.Register(
            nameof(Speakers),
            typeof(ObservableCollection<SpeakerViewModel>),
            typeof(SpeakerLayoutCanvas),
            new PropertyMetadata(null, OnSpeakersChanged));

    public static readonly DependencyProperty SelectedSpeakerProperty =
        DependencyProperty.Register(
            nameof(SelectedSpeaker),
            typeof(SpeakerViewModel),
            typeof(SpeakerLayoutCanvas),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedSpeakerChanged));

    public static readonly DependencyProperty SpeakerMovedCommandProperty =
        DependencyProperty.Register(
            nameof(SpeakerMovedCommand),
            typeof(ICommand),
            typeof(SpeakerLayoutCanvas),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AllowMoveProperty =
        DependencyProperty.Register(
            nameof(AllowMove),
            typeof(bool),
            typeof(SpeakerLayoutCanvas),
            new PropertyMetadata(true));

    private SpeakerViewModel? _dragging;
    private Point _dragOffset;
    private bool _moved;

    private static readonly Brush RoomBrush = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
    private static readonly Brush GridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
    private static readonly Brush SpeakerBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xC0));
    private static readonly Brush SpeakerSelectedBrush = new SolidColorBrush(Color.FromRgb(0xC4, 0x79, 0x00));
    private static readonly Brush SpeakerHeightBrush = new SolidColorBrush(Color.FromRgb(0x7A, 0x5C, 0xC8));
    private static readonly Brush ListenerBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0x5D, 0x5D, 0x5D));

    static SpeakerLayoutCanvas()
    {
        RoomBrush.Freeze();
        GridBrush.Freeze();
        SpeakerBrush.Freeze();
        SpeakerSelectedBrush.Freeze();
        SpeakerHeightBrush.Freeze();
        ListenerBrush.Freeze();
        TextBrush.Freeze();
    }

    public ObservableCollection<SpeakerViewModel>? Speakers
    {
        get => (ObservableCollection<SpeakerViewModel>?)GetValue(SpeakersProperty);
        set => SetValue(SpeakersProperty, value);
    }

    public SpeakerViewModel? SelectedSpeaker
    {
        get => (SpeakerViewModel?)GetValue(SelectedSpeakerProperty);
        set => SetValue(SelectedSpeakerProperty, value);
    }

    public ICommand? SpeakerMovedCommand
    {
        get => (ICommand?)GetValue(SpeakerMovedCommandProperty);
        set => SetValue(SpeakerMovedCommandProperty, value);
    }

    /// <summary>false 时只允许点击选中，禁止拖拽。</summary>
    public bool AllowMove
    {
        get => (bool)GetValue(AllowMoveProperty);
        set => SetValue(AllowMoveProperty, value);
    }

    private static void OnSpeakersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SpeakerLayoutCanvas canvas)
            return;

        if (e.OldValue is ObservableCollection<SpeakerViewModel> oldCol)
        {
            oldCol.CollectionChanged -= canvas.OnCollectionChanged;
            foreach (var s in oldCol)
                s.PropertyChanged -= canvas.OnSpeakerPropertyChanged;
        }
        if (e.NewValue is ObservableCollection<SpeakerViewModel> newCol)
        {
            newCol.CollectionChanged += canvas.OnCollectionChanged;
            foreach (var s in newCol)
                s.PropertyChanged += canvas.OnSpeakerPropertyChanged;
        }

        canvas.InvalidateVisual();
    }

    private static void OnSelectedSpeakerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SpeakerLayoutCanvas canvas)
            return;

        if (canvas.Speakers is not null)
        {
            foreach (var s in canvas.Speakers)
                s.IsSelected = ReferenceEquals(s, e.NewValue);
        }
        canvas.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (SpeakerViewModel s in e.NewItems)
                s.PropertyChanged += OnSpeakerPropertyChanged;
        }
        if (e.OldItems is not null)
        {
            foreach (SpeakerViewModel s in e.OldItems)
                s.PropertyChanged -= OnSpeakerPropertyChanged;
        }
        InvalidateVisual();
    }

    private void OnSpeakerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SpeakerViewModel.IsSelected)
            or nameof(SpeakerViewModel.X)
            or nameof(SpeakerViewModel.Y))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 40 || h < 40)
            return;

        var room = new Rect(8, 8, w - 16, h - 16);
        dc.DrawRoundedRectangle(RoomBrush, new Pen(GridBrush, 1.5), room, 12, 12);

        var gridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)) { Opacity = 0.45 };
        var pen = new Pen(gridBrush, 1);
        for (int i = 1; i < 4; i++)
        {
            double x = room.X + room.Width * i / 4.0;
            double y = room.Y + room.Height * i / 4.0;
            dc.DrawLine(pen, new Point(x, room.Y), new Point(x, room.Bottom));
            dc.DrawLine(pen, new Point(room.X, y), new Point(room.Right, y));
        }

        var frontLabel = new FormattedText(
            "前方（屏幕）", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            12, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(frontLabel, new Point(room.X + 10, room.Y + 6));

        var listener = new Point(room.X + room.Width * 0.5, room.Y + room.Height * 0.55);
        dc.DrawEllipse(ListenerBrush, null, listener, 16, 16);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A)), 2), listener, 16, 16);
        var listenerLabel = new FormattedText(
            "听众", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            11, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(listenerLabel, new Point(listener.X - listenerLabel.Width / 2, listener.Y + 20));

        if (Speakers is null)
            return;

        foreach (var speaker in Speakers)
        {
            double cx = room.X + room.Width * speaker.X;
            double cy = room.Y + room.Height * speaker.Y;
            bool sel = speaker.IsSelected || ReferenceEquals(speaker, SelectedSpeaker);
            double radius = sel ? 18 : 15;

            var fill = sel ? SpeakerSelectedBrush
                : speaker.HeightTier > 0 ? SpeakerHeightBrush
                : SpeakerBrush;

            if (sel)
            {
                var glow = new Pen(new SolidColorBrush(Color.FromRgb(0xC4, 0x79, 0x00)), 2);
                dc.DrawEllipse(null, glow, new Point(cx, cy), radius + 7, radius + 7);
            }

            dc.DrawEllipse(fill, null, new Point(cx, cy), radius, radius);

            var label = new FormattedText(
                speaker.ShortLabel,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                11, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(label, new Point(cx - label.Width / 2, cy - label.Height / 2));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var hit = HitTestSpeaker(e.GetPosition(this));
        if (hit is null)
            return;

        SelectedSpeaker = hit;
        if (Speakers is not null)
        {
            foreach (var s in Speakers)
                s.IsSelected = ReferenceEquals(s, hit);
        }
        InvalidateVisual();
        e.Handled = true;

        if (!AllowMove)
            return;

        _dragging = hit;
        _moved = false;
        var p = e.GetPosition(this);
        _dragOffset = new Point(p.X - XToPixel(hit.X), p.Y - YToPixel(hit.Y));
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging is null || !IsMouseCaptured || !AllowMove)
            return;

        var p = e.GetPosition(this);
        _dragging.X = PixelToX(p.X - _dragOffset.X);
        _dragging.Y = PixelToY(p.Y - _dragOffset.Y);
        _moved = true;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragging is not null)
        {
            if (_moved)
                SpeakerMovedCommand?.Execute(_dragging);
            _dragging = null;
        }
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        e.Handled = true;
    }

    private SpeakerViewModel? HitTestSpeaker(Point p)
    {
        if (Speakers is null)
            return null;

        foreach (var speaker in Speakers.Reverse())
        {
            double cx = XToPixel(speaker.X);
            double cy = YToPixel(speaker.Y);
            double dx = p.X - cx;
            double dy = p.Y - cy;
            if (dx * dx + dy * dy <= 22 * 22)
                return speaker;
        }
        return null;
    }

    private double XToPixel(double nx) => 8 + (ActualWidth - 16) * nx;
    private double YToPixel(double ny) => 8 + (ActualHeight - 16) * ny;
    private double PixelToX(double px) => Math.Clamp((px - 8) / Math.Max(1, ActualWidth - 16), 0.02, 0.98);
    private double PixelToY(double py) => Math.Clamp((py - 8) / Math.Max(1, ActualHeight - 16), 0.02, 0.98);
}
