namespace MatroskaBatchFlow.Uno.Presentation.Controls;

public sealed partial class InputOperationOverlay : UserControl
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(InputOperationOverlay), new PropertyMetadata(false));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(InputOperationOverlay), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(InputOperationOverlay), new PropertyMetadata(true));

    public static readonly DependencyProperty CurrentProperty =
        DependencyProperty.Register(nameof(Current), typeof(int), typeof(InputOperationOverlay), new PropertyMetadata(0));

    public static readonly DependencyProperty TotalProperty =
        DependencyProperty.Register(nameof(Total), typeof(int), typeof(InputOperationOverlay), new PropertyMetadata(0));

    public static readonly DependencyProperty BlocksInputProperty =
        DependencyProperty.Register(nameof(BlocksInput), typeof(bool), typeof(InputOperationOverlay), new PropertyMetadata(false));

    public InputOperationOverlay()
    {
        this.InitializeComponent();
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public int Current
    {
        get => (int)GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public int Total
    {
        get => (int)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public bool BlocksInput
    {
        get => (bool)GetValue(BlocksInputProperty);
        set => SetValue(BlocksInputProperty, value);
    }
}
