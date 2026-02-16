using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Adapts and synchronizes the core file list (ScannedFileInfo) with a UI-facing collection of ScannedFileViewModel.
/// Ensures both collections stay in sync, regardless of where changes originate.
/// </summary>
public partial class FileListAdapter : IFileListAdapter, IDisposable
{
    /// <summary>
    /// The logger instance for recording file list operations.
    /// </summary>
    private readonly ILogger<FileListAdapter> _logger;

    /// <summary>
    /// The collection of view-models for UI binding.
    /// </summary>
    public ObservableCollection<ScannedFileViewModel> ScannedFileViewModels { get; }

    /// <summary>
    /// The core collection of scanned files.
    /// </summary>
    public UniqueObservableCollection<ScannedFileInfo> CoreList => _batchConfig.FileList;

    /// <summary>
    /// The batch configuration that contains the core file list and manages the scanned files.
    /// </summary>
    private readonly IBatchConfiguration _batchConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileListAdapter"/> class and sets up synchronization.
    /// </summary>
    /// <param name="batchConfig">The batch configuration containing the core file list.</param>
    /// <param name="logger">The logger instance for recording file list operations.</param>
    public FileListAdapter(IBatchConfiguration batchConfig, ILogger<FileListAdapter> logger)
    {
        _batchConfig = batchConfig;
        _logger = logger;
        ScannedFileViewModels = new ObservableCollection<ScannedFileViewModel>(
            batchConfig.FileList.Select(f => new ScannedFileViewModel(f, batchConfig))
        );

        // Sync: CoreList -> ScannedFileViewModels
        _batchConfig.FileList.CollectionChanged += OnCoreListChanged;
        // Sync: ScannedFileViewModels -> CoreList
        ScannedFileViewModels.CollectionChanged += OnViewModelsChanged;
    }

    /// <summary>
    /// Handles changes to the underlying collection of scanned files and updates the corresponding view models accordingly.
    /// </summary>
    /// <param name="sender">The source of the event. This parameter is typically the collection that triggered the change.</param>
    /// <param name="eventArgs">The event data containing details about the change, such as the action performed and the affected items.</param>
    private void OnCoreListChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        switch (eventArgs.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ScannedFileInfo file in eventArgs.NewItems!)
                    if (!ScannedFileViewModels.Any(vm => vm.FileInfo == file))
                        ScannedFileViewModels.Add(new ScannedFileViewModel(file, _batchConfig));
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ScannedFileInfo file in eventArgs.OldItems!)
                {
                    var vm = ScannedFileViewModels.FirstOrDefault(x => x.FileInfo == file);
                    if (vm is not null)
                        ScannedFileViewModels.Remove(vm);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ScannedFileViewModels.Clear();
                break;
            case NotifyCollectionChangedAction.Replace:
                foreach (ScannedFileInfo file in eventArgs.OldItems!)
                {
                    var vm = ScannedFileViewModels.FirstOrDefault(x => x.FileInfo == file);
                    if (vm != null)
                        ScannedFileViewModels.Remove(vm);
                }
                foreach (ScannedFileInfo file in eventArgs.NewItems!)
                    if (!ScannedFileViewModels.Any(vm => vm.FileInfo == file))
                        ScannedFileViewModels.Add(new ScannedFileViewModel(file, _batchConfig));
                break;
        }
    }

    /// <summary>
    /// Handles changes to the collection of view models and updates the associated file list accordingly.
    /// </summary>
    /// <param name="sender">The source of the event. This parameter is typically the collection that triggered the change.</param>
    /// <param name="eventArgs">The event data containing details about the collection change, including the action performed  and the items
    /// involved in the change.</param>
    private void OnViewModelsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        switch (eventArgs.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ScannedFileViewModel vm in eventArgs.NewItems!)
                    if (!_batchConfig.FileList.Contains(vm.FileInfo))
                        _batchConfig.FileList.Add(vm.FileInfo);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ScannedFileViewModel vm in eventArgs.OldItems!)
                    _batchConfig.FileList.Remove(vm.FileInfo);
                break;
            case NotifyCollectionChangedAction.Reset:
                _batchConfig.FileList.Clear();
                break;
            case NotifyCollectionChangedAction.Replace:
                foreach (ScannedFileViewModel vm in eventArgs.OldItems!)
                    _batchConfig.FileList.Remove(vm.FileInfo);
                foreach (ScannedFileViewModel vm in eventArgs.NewItems!)
                    if (!_batchConfig.FileList.Contains(vm.FileInfo))
                        _batchConfig.FileList.Add(vm.FileInfo);
                break;
        }
    }

    /// <summary>
    /// Adds a scanned file to the batch configuration's file list.
    /// </summary>
    /// <param name="file">The scanned file information to add. Cannot be null.</param>
    public void AddFile(ScannedFileInfo file) => _batchConfig.FileList.Add(file);

    /// <summary>
    /// Adds a collection of files to the current batch configuration.
    /// </summary>
    /// <remarks>This method appends the provided files to the existing file list in the batch
    /// configuration.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to be added.
    /// Cannot be <see langword="null"/>.</param>
    public void AddFiles(IEnumerable<ScannedFileInfo> files)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
            return;

        int countBefore = _batchConfig.FileList.Count;
        _batchConfig.FileList.AddRange(fileList);
        int addedCount = _batchConfig.FileList.Count - countBefore;

        LogFilesAdded(addedCount);
    }

    /// <summary>
    /// Removes the specified file from the batch configuration's file list.
    /// </summary>
    /// <param name="file">The file to be removed. Cannot be <see langword="null"/>.</param>
    public void RemoveFile(ScannedFileInfo file)
    {
        LogRemovingFile(file.Path);
        _batchConfig.FileList.Remove(file);
    }

    /// <summary>
    /// Removes a collection of files from the batch configuration's file list.
    /// </summary>
    /// <param name="files">The files to remove.</param>
    public void RemoveFiles(IEnumerable<ScannedFileInfo> files)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
            return;

        LogRemovingFiles(fileList.Count);
        _batchConfig.FileList.RemoveRange(fileList);
    }

    /// <summary>
    /// Removes the specified view model and its associated file from the collection.
    /// </summary>
    /// <param name="vm">The view model representing the file to be removed. Cannot be <see langword="null"/>.</param>
    public void RemoveViewModel(ScannedFileViewModel vm) => RemoveFile(vm.FileInfo);

    /// <summary>
    /// Clears both the core and view-model collections.
    /// </summary>
    public void Clear()
    {
        LogClearingFiles();
        _batchConfig.FileList.Clear();
    }

    /// <summary>
    /// Rebuilds the view-model collection from the current core file list.
    /// </summary>
    public void RebuildFromCore()
    {
        ScannedFileViewModels.Clear();

        foreach (var file in _batchConfig.FileList)
            ScannedFileViewModels.Add(new ScannedFileViewModel(file, _batchConfig));
    }

    /// <summary>
    /// Unsubscribes from collection events to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        _batchConfig.FileList.CollectionChanged -= OnCoreListChanged;
        ScannedFileViewModels.CollectionChanged -= OnViewModelsChanged;
        GC.SuppressFinalize(this); //CA1816: Dispose methods should call SuppressFinalize to prevent finalization.
    }
}
