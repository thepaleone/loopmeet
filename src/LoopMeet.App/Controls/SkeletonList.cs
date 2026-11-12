using Microsoft.Maui.Controls.Shapes;

namespace LoopMeet.App.Controls;

/// <summary>The card a placeholder imitates. Each matches the real card's style, padding and size.</summary>
public enum SkeletonVariant
{
    /// <summary>Home's upcoming meetup card: title, time, location and group lines.</summary>
    HomeMeetupCard,

    /// <summary>Group detail's compact meetup card: title, time and location lines.</summary>
    GroupMeetupCard,

    /// <summary>Group detail's member tile: an avatar circle over a name, laid out in wrapping rows.</summary>
    MemberTile,

    /// <summary>The Groups list card: an owner avatar circle beside the group name.</summary>
    GroupCard
}

/// <summary>
/// Shows <see cref="Count"/> placeholder cards of one <see cref="Variant"/> with a sweeping shimmer
/// while <see cref="IsActive"/> is true. Bind IsActive to a section's Show…Skeleton property
/// (see LoadingPlaceholders). The shimmer runs only while the list is active and loaded.
/// </summary>
public sealed class SkeletonList : ContentView
{
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(
        nameof(Variant), typeof(SkeletonVariant), typeof(SkeletonList), SkeletonVariant.GroupMeetupCard,
        propertyChanged: (bindable, _, _) => ((SkeletonList)bindable).Rebuild());

    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count), typeof(int), typeof(SkeletonList), 2,
        propertyChanged: (bindable, _, _) => ((SkeletonList)bindable).Rebuild());

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(SkeletonList), false,
        propertyChanged: (bindable, _, _) => ((SkeletonList)bindable).UpdateActivity());

    private const double ShimmerBandWidth = 72;
    private readonly List<(VisualElement Band, int Delay)> _shimmerBands = new();
    private int _cardIndex;
    private CancellationTokenSource? _shimmerCts;

    public SkeletonList()
    {
        IsVisible = false;
        Loaded += (_, _) => UpdateActivity();
        Unloaded += (_, _) => StopShimmer();
        Rebuild();
    }

    public SkeletonVariant Variant
    {
        get => (SkeletonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private void Rebuild()
    {
        StopShimmer();
        _shimmerBands.Clear();
        _cardIndex = 0;

        Layout container = Variant == SkeletonVariant.MemberTile
            ? new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row }
            : new VerticalStackLayout { Spacing = 8 };

        for (var i = 0; i < Math.Max(Count, 0); i++)
        {
            container.Children.Add(BuildCard());
        }

        Content = container;
        UpdateActivity();
    }

    private void UpdateActivity()
    {
        IsVisible = IsActive;
        if (IsActive && IsLoaded)
        {
            StartShimmer();
        }
        else
        {
            StopShimmer();
        }
    }

    private View BuildCard()
    {
        // Counted before the card's shapes are built, so they all take this card's shimmer delay.
        _cardIndex++;
        return Variant switch
        {
            SkeletonVariant.HomeMeetupCard => Card("BubbleCardBorderStyle", new Thickness(16), new Thickness(0), Lines(4, 180, 120, 150, 90)),
            SkeletonVariant.GroupMeetupCard => Card("BubbleCardCompactBorderStyle", new Thickness(12, 10), new Thickness(0, 0, 0, 4), Lines(3, 170, 110, 140)),
            SkeletonVariant.MemberTile => Card("BubbleCardCompactBorderStyle", new Thickness(10, 8), new Thickness(0, 0, 8, 8), MemberTileContent()),
            SkeletonVariant.GroupCard => Card("BubbleCardCompactBorderStyle", new Thickness(12, 10), new Thickness(0, 0, 0, 4), GroupCardContent()),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown skeleton variant.")
        };
    }

    // The real cards' line heights for 15 pt bold titles and 13/12 pt detail text.
    private View Lines(int count, params double[] widths)
    {
        var lineHeights = new[] { 20d, 17d, 17d, 16d };
        var barHeights = new[] { 14d, 11d, 11d, 10d };
        var stack = new VerticalStackLayout { Spacing = 4 };
        for (var i = 0; i < count; i++)
        {
            stack.Children.Add(new Grid
            {
                HeightRequest = lineHeights[i],
                Children = { Bar(widths[i], barHeights[i]) }
            });
        }

        return stack;
    }

    private View MemberTileContent() => new VerticalStackLayout
    {
        Spacing = 6,
        HorizontalOptions = LayoutOptions.Center,
        Children =
        {
            Circle(40),
            new Grid { HeightRequest = 16, Children = { Bar(48, 10, LayoutOptions.Center) } }
        }
    };

    private View GroupCardContent()
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(54), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
            VerticalOptions = LayoutOptions.Center
        };
        // 41 rather than the real avatar's 48: at full size the placeholder card stood taller than
        // the real one (checked on the simulator).
        grid.Add(Circle(41), 0);
        grid.Add(Bar(140, 14), 1);
        return grid;
    }

    private static View Card(string styleKey, Thickness padding, Thickness margin, View content)
    {
        var card = new Border
        {
            Padding = padding,
            Margin = margin,
            InputTransparent = true,
            Content = content
        };
        if (Application.Current?.Resources.TryGetValue(styleKey, out var style) == true && style is Style borderStyle)
        {
            card.Style = borderStyle;
        }

        return card;
    }

    // A text-line placeholder. It clips its own shimmer band, so only the bar shimmers, not the card.
    private Border Bar(double width, double height, LayoutOptions? horizontal = null) =>
        Shape(new RoundRectangle { CornerRadius = height / 2 }, width, height, horizontal ?? LayoutOptions.Start);

    // An avatar placeholder, with its own clipped shimmer band.
    private Border Circle(double size) => Shape(new Ellipse(), size, size, LayoutOptions.Center);

    private Border Shape(Shape strokeShape, double width, double height, LayoutOptions horizontal)
    {
        var band = new BoxView
        {
            WidthRequest = Math.Min(ShimmerBandWidth, width / 2),
            HorizontalOptions = LayoutOptions.Start,
            InputTransparent = true,
            Opacity = 0.47
        };
        band.SetAppThemeColor(BoxView.ColorProperty, ThemeColor("White", Colors.White), ThemeColor("Gray500", Colors.Gray));
        // Every shape in a card sweeps together; cards are staggered, as the Groups list's were.
        _shimmerBands.Add((band, 40 + (_cardIndex * 90 % 160)));

        var shape = new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            StrokeThickness = 0,
            StrokeShape = strokeShape,
            HorizontalOptions = horizontal,
            VerticalOptions = LayoutOptions.Center,
            Content = band
        };
        shape.SetAppThemeColor(BackgroundColorProperty, ThemeColor("Gray200", Colors.LightGray), ThemeColor("Gray600", Colors.DimGray));
        return shape;
    }

    private static Color ThemeColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;

    private void StartShimmer()
    {
        StopShimmer();
        _shimmerCts = new CancellationTokenSource();
        var token = _shimmerCts.Token;
        foreach (var (band, delay) in _shimmerBands)
        {
            _ = AnimateAsync(band, token, delay);
        }
    }

    private void StopShimmer()
    {
        _shimmerCts?.Cancel();
        _shimmerCts = null;
        foreach (var (band, _) in _shimmerBands)
        {
            band.CancelAnimations();
        }
    }

    // The sweep the Groups list used: across each shape, a short pause, and again.
    private static async Task AnimateAsync(VisualElement band, CancellationToken token, int initialDelay)
    {
        try
        {
            await Task.Delay(initialDelay, token);
            while (!token.IsCancellationRequested)
            {
                // From just off the shape's left edge to just past its right edge; the shape clips it.
                band.TranslationX = -band.WidthRequest;
                await band.TranslateToAsync((band.Parent as VisualElement)?.Width ?? 0, 0, 900, Easing.Linear);
                await Task.Delay(150, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
