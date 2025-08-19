using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class GeneralViewModel : ObservableObject
{
    private readonly IBatchConfiguration _batchConfiguration;

    public string Title
    {
        get => _batchConfiguration.Title;
        set
        {
            if (_batchConfiguration.Title != value)
            {
                _batchConfiguration.Title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public bool ShouldModifyTitle
    {
        get => _batchConfiguration.ShouldModifyTitle;
        set
        {
            if (_batchConfiguration.ShouldModifyTitle != value)
            {
                _batchConfiguration.ShouldModifyTitle = value;
                OnPropertyChanged(nameof(ShouldModifyTitle));
            }
        }
    }

    public bool AddTrackStatisticsTags
    {
        get => _batchConfiguration.AddTrackStatisticsTags;
        set
        {
            if (_batchConfiguration.AddTrackStatisticsTags != value)
            {
                _batchConfiguration.AddTrackStatisticsTags = value;
                OnPropertyChanged(nameof(AddTrackStatisticsTags));
            }
        }
    }

    public bool DeleteTrackStatisticsTags
    {
        get => _batchConfiguration.DeleteTrackStatisticsTags;
        set
        {
            if (_batchConfiguration.DeleteTrackStatisticsTags != value)
            {
                _batchConfiguration.DeleteTrackStatisticsTags = value;
                OnPropertyChanged(nameof(DeleteTrackStatisticsTags));
            }
        }
    }

    public bool ShouldModifyTrackStatisticsTags
    {
        get => _batchConfiguration.ShouldModifyTrackStatisticsTags;
        set
        {
            if (_batchConfiguration.ShouldModifyTrackStatisticsTags != value)
            {
                _batchConfiguration.ShouldModifyTrackStatisticsTags = value;
                OnPropertyChanged(nameof(ShouldModifyTrackStatisticsTags));
            }
        }

    }

    /// <summary>
    /// Gets a value indicating whether the general page is enabled based on whether files have been added to the program.
    /// </summary>
    public bool IsFileListPopulated => _batchConfiguration.FileList.Count > 0;

    public GeneralViewModel(IBatchConfiguration batchConfiguration)
    {
        _batchConfiguration = batchConfiguration;

        SetupEventHandlers();
    }

    /// <summary>
    /// Sets up event handlers for monitoring changes in the batch configuration.
    /// </summary>
    private void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;

        // Subscribe to FileList changes to update IsFileListPopulated property
        _batchConfiguration.FileList.CollectionChanged += OnFileListChanged;
    }

    /// <summary>
    /// Handles changes to the FileList collection to update the IsFileListPopulated property.
    /// </summary>
    /// <param name="sender">The source of the event, typically the FileList collection.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection.</param>
    private void OnFileListChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(IsFileListPopulated));
    }

    /// <summary>
    /// Handles changes to the batch configuration and updates the relevant properties accordingly.
    /// </summary>
    /// <param name="sender">The source of the event. This parameter is typically the batch configuration object.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed. The <see
    /// cref="PropertyChangedEventArgs.PropertyName"/> must not be <see langword="null"/>.</param>
    private void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(eventArgs.PropertyName))
            return;

        if (eventArgs.PropertyName == nameof(IBatchConfiguration.Title))
            OnPropertyChanged(nameof(Title));
    }
}
