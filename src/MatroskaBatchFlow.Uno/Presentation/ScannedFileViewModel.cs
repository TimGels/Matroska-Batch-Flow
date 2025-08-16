using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a view model for a scanned file, providing access to file details and functionality to remove the file
/// from a batch.
/// </summary>
/// <remarks>This class is designed to be used in scenarios where a scanned file needs to be displayed and managed
/// within a batch processing context, such as in a ListView or similar UI component.</remarks>
public class ScannedFileViewModel(ScannedFileInfo file, IBatchConfiguration batchConfig)
{
    public ScannedFileInfo File { get; } = file;
    public ICommand RemoveSelf { get; } = new RelayCommand(() => batchConfig.FileList.Remove(file));
}
