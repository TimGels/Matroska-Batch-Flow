namespace MatroskaBatchFlow.Uno.Presentation.Controls;

public sealed partial class SummaryTileControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SummaryTileControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(SummaryTileControl), new PropertyMetadata(0));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public SummaryTileControl()
    {
        this.InitializeComponent();
    }
}
