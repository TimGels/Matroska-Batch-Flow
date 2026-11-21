using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Presentation.Controls;

public sealed partial class TrackEditScope : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(TrackViewModelBase), typeof(TrackEditScope), new PropertyMetadata(null));

    public TrackViewModelBase? ViewModel
    {
        get => (TrackViewModelBase?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public TrackEditScope() => InitializeComponent();

    private void ReturnToCheck_Click(object sender, RoutedEventArgs e) => App.GetService<INavigationService>().GoBack();
}
