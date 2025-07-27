using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class ShellViewModel(INavigationService navigationServicem) : ObservableObject
{
    public INavigationService NavigationService { get; } = navigationServicem;
}
