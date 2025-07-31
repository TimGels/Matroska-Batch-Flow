using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class OutputViewModel(IBatchConfiguration batchConfiguration) : ObservableObject
{
    [ObservableProperty]
    private IBatchConfiguration _batchConfiguration = batchConfiguration;
}
