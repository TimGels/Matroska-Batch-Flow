using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a view model for a scanned fileInfo, providing access to fileInfo details and functionality to remove the fileInfo
/// from a batch.
/// </summary>
/// <remarks>This class is designed to be used in scenarios where a scanned fileInfo needs to be displayed and managed
/// within a batch processing context, such as in a ListView or similar UI component.</remarks>
public class ScannedFileViewModel(ScannedFileInfo fileInfo, IBatchConfiguration batchConfig)
{
    public ScannedFileInfo FileInfo { get; } = fileInfo;
    public ICommand RemoveSelf { get; } = new RelayCommand(() => batchConfig.FileList.Remove(fileInfo));
}
