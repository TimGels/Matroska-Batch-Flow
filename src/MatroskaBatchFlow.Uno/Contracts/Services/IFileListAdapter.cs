using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Utilities;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

public interface IFileListAdapter
{
    ObservableCollection<ScannedFileViewModel> ScannedFileViewModels { get; }
    UniqueObservableCollection<ScannedFileInfo> CoreList { get; }

    void AddFile(ScannedFileInfo file);
    void AddFiles(IEnumerable<ScannedFileInfo> files);
    void RemoveFile(ScannedFileInfo file);
    void RemoveViewModel(ScannedFileViewModel vm);
    void Clear();
    void RebuildFromCore();
}
