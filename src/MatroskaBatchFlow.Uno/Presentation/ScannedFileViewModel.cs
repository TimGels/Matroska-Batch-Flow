using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a view model for a scanned fileInfo, providing access to fileInfo details and its file check indicators.
/// </summary>
/// <remarks>
/// This class is designed to be used in scenarios where a scanned fileInfo needs to be displayed and managed
/// within a batch processing context, such as in a ListView or similar UI component. Removal goes through
/// <see cref="InputViewModel"/>, so the batch is revalidated afterwards.
/// </remarks>
public partial class ScannedFileViewModel(ScannedFileInfo fileInfo) : ObservableObject
{
    /// <summary>
    /// Gets the scanned file.
    /// </summary>
    public ScannedFileInfo FileInfo { get; } = fileInfo;

    /// <summary>
    /// Gets the file name with its extension, without the directory.
    /// </summary>
    public string FileName => Path.GetFileName(FileInfo.Path);

    /// <summary>
    /// Gets the directory that contains the file, or an empty string when the path has none.
    /// </summary>
    public string Directory => Path.GetDirectoryName(FileInfo.Path) ?? string.Empty;

    /// <summary>
    /// Gets or sets the checks this file participates in, including comparisons where its value matches.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFileChecks))]
    [NotifyPropertyChangedFor(nameof(FileCheckCount))]
    public partial IReadOnlyList<FileCheckKey> CheckTopics { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the file is unsupported, which shows the Unsupported file label in the file list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsUnsupported { get; set; }

    /// <summary>
    /// Gets the number of checks this file participates in, shown in the Batch checks column.
    /// </summary>
    public int FileCheckCount => CheckTopics.Count;

    /// <summary>
    /// Gets whether the file participates in any check.
    /// </summary>
    public bool HasFileChecks => FileCheckCount > 0;

    /// <summary>
    /// Returns the file name.
    /// </summary>
    /// <returns>The file name with its extension.</returns>
    public override string ToString() => FileName;
}
