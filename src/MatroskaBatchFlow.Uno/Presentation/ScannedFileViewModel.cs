using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a view model for a scanned fileInfo, providing access to fileInfo details and functionality to remove the fileInfo
/// from a batch.
/// </summary>
/// <remarks>This class is designed to be used in scenarios where a scanned fileInfo needs to be displayed and managed
/// within a batch processing context, such as in a ListView or similar UI component.</remarks>
public partial class ScannedFileViewModel(ScannedFileInfo fileInfo, IBatchConfiguration batchConfig) : ObservableObject
{
    public ScannedFileInfo FileInfo { get; } = fileInfo;
    public string FileName => Path.GetFileName(FileInfo.Path);
    public string Directory => Path.GetDirectoryName(FileInfo.Path) ?? string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFileChecks))]
    [NotifyPropertyChangedFor(nameof(FileCheckCount))]
    public partial IReadOnlyList<FileCheckKey> CheckTopics { get; set; } = [];

    [ObservableProperty]
    public partial bool IsUnsupported { get; set; }

    public int FileCheckCount => CheckTopics.Count;
    public bool HasFileChecks => FileCheckCount > 0;
    public ICommand RemoveSelf { get; } = new RelayCommand(() => batchConfig.FileList.Remove(fileInfo));
    public override string ToString() => FileName;
}
