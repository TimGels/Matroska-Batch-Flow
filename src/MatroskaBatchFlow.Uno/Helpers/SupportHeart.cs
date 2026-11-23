using Microsoft.UI.Xaml.Input;

namespace MatroskaBatchFlow.Uno.Helpers;

/// <summary>
/// Fills a support heart icon while the pointer is over its host, such as a navigation item
/// or a settings card.
/// </summary>
/// <remarks>
/// Usage in XAML, on the element the pointer hovers over:
/// <c>helpers:SupportHeart.Icon="{x:Bind HeartIcon}"</c>, where <c>HeartIcon</c> is the outline heart <see cref="FontIcon"/> inside it.
/// Set <see cref="IsHighlightedProperty"/> to keep the heart filled without hovering, for example while its panel is open.
/// The pink is the <c>SupportHeartBrush</c> application resource.
/// </remarks>
public static class SupportHeart
{
    private const string OutlineGlyph = "\uEB51";
    private const string FilledGlyph = "\uEB52";
    private const string BrushKey = "SupportHeartBrush";

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached("Icon", typeof(FontIcon), typeof(SupportHeart), new PropertyMetadata(null, OnIconChanged));

    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.RegisterAttached("IsHighlighted", typeof(bool), typeof(SupportHeart), new PropertyMetadata(false, OnStateChanged));

    private static readonly DependencyProperty IsPointerOverProperty =
        DependencyProperty.RegisterAttached("IsPointerOver", typeof(bool), typeof(SupportHeart), new PropertyMetadata(false, OnStateChanged));

    private static readonly PointerEventHandler PointerEnteredHandler = (sender, _) => ((DependencyObject)sender).SetValue(IsPointerOverProperty, true);

    private static readonly PointerEventHandler PointerExitedHandler = (sender, _) => ((DependencyObject)sender).SetValue(IsPointerOverProperty, false);

    public static FontIcon? GetIcon(UIElement host) => (FontIcon?)host.GetValue(IconProperty);

    public static void SetIcon(UIElement host, FontIcon? value) => host.SetValue(IconProperty, value);

    public static bool GetIsHighlighted(UIElement host) => (bool)host.GetValue(IsHighlightedProperty);

    public static void SetIsHighlighted(UIElement host, bool value) => host.SetValue(IsHighlightedProperty, value);

    private static void OnIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not UIElement host)
            return;

        host.RemoveHandler(UIElement.PointerEnteredEvent, PointerEnteredHandler);
        host.RemoveHandler(UIElement.PointerExitedEvent, PointerExitedHandler);

        if (args.NewValue is not null)
        {
            // Hosts handle pointer events for their own visual states, so also listen to handled events.
            host.AddHandler(UIElement.PointerEnteredEvent, PointerEnteredHandler, handledEventsToo: true);
            host.AddHandler(UIElement.PointerExitedEvent, PointerExitedHandler, handledEventsToo: true);
        }

        Update(host);
    }

    private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is UIElement host)
        {
            Update(host);
        }
    }

    /// <summary>
    /// Shows the filled heart while the pointer is over the host or the host is highlighted, and the outline otherwise.
    /// </summary>
    /// <param name="host">The element that shows the heart.</param>
    private static void Update(UIElement host)
    {
        if (GetIcon(host) is not { } icon)
            return;

        if ((bool)host.GetValue(IsPointerOverProperty) || GetIsHighlighted(host))
        {
            icon.Glyph = FilledGlyph;
            icon.Foreground = (Brush)Application.Current.Resources[BrushKey];
        }
        else
        {
            icon.Glyph = OutlineGlyph;
            icon.ClearValue(IconElement.ForegroundProperty);
        }
    }
}
