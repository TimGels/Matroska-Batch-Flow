using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Represents a report for the processing of a single file.
/// </summary>
public class FileProcessingReport : INotifyPropertyChanged
{
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private TimeSpan? _duration;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _finishedAt;
    private string? _executedCommand;
    private string? _notes;

    private readonly ObservableCollection<string> _warnings = [];
    private readonly ObservableCollection<string> _errors = [];

    /// <summary>
    /// Scanned file metadata associated with this processing report.
    /// </summary>
    public required ScannedFileInfo SourceFile { get; init; }

    public Guid Id { get; } = Guid.NewGuid();
    public string Path { get; init; } = string.Empty;

    public ProcessingStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public DateTimeOffset? StartedAt
    {
        get => _startedAt;
        set
        {
            if (_startedAt == value)
                return;

            _startedAt = value;
            OnPropertyChanged(nameof(StartedAt));
        }
    }

    public DateTimeOffset? FinishedAt
    {
        get => _finishedAt;
        set
        {
            if (_finishedAt == value)
                return;

            _finishedAt = value;
            OnPropertyChanged(nameof(FinishedAt));
        }
    }

    public TimeSpan? Duration
    {
        get => _duration;
        set
        {
            if (_duration == value)
                return;

            _duration = value;
            OnPropertyChanged(nameof(Duration));
        }
    }

    public string? ExecutedCommand
    {
        get => _executedCommand;
        set
        {
            if (_executedCommand == value)
                return;

            _executedCommand = value;
            OnPropertyChanged(nameof(ExecutedCommand));
        }
    }

    /// <summary>
    /// Gets or sets the notes associated with the object. 
    /// This property can be used to store additional information or comments.
    /// </summary>
    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes == value)
                return;

            _notes = value;
            OnPropertyChanged(nameof(Notes));
        }
    }

    public ReadOnlyObservableCollection<string> Warnings { get; }
    public ReadOnlyObservableCollection<string> Errors { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessingReport"/> class.
    /// </summary>
    public FileProcessingReport()
    {
        Warnings = new ReadOnlyObservableCollection<string>(_warnings);
        Errors = new ReadOnlyObservableCollection<string>(_errors);
    }

    /// <summary>
    /// Adds a warning message to the collection of warnings.
    /// </summary>
    /// <remarks>If the message is null, empty, or consists only of whitespace, it is ignored.</remarks>
    /// <param name="message">The warning message to add. Cannot be null, empty, or consist only of whitespace.</param>
    public void AddWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _warnings.Add(message);
        OnPropertyChanged(nameof(Warnings));
        UpdateStatusForIssues();
    }

    /// <summary>
    /// Adds the specified collection of warning messages to the internal list of warnings.
    /// </summary>
    /// <remarks>If any messages are null, empty, or consist only of whitespace, they are ignored.</remarks>
    /// <param name="messages">A collection of warning messages to add. Messages that are <see langword="null"/>, empty, or consist only of
    /// whitespace are ignored.</param>
    public void AddWarnings(IEnumerable<string> messages)
    {
        var anyAdded = false;

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message))
                continue;

            _warnings.Add(message);
            anyAdded = true;
        }

        if (anyAdded)
        {
            OnPropertyChanged(nameof(Warnings));
            UpdateStatusForIssues();
        }
    }

    /// <summary>
    /// Adds one or more warning messages to the current context.
    /// </summary>
    /// <remarks>If any messages are null, empty, or consist only of whitespace, they are ignored.</remarks>
    /// <param name="messages">An array of warning messages to add. Each message must be a non-null, non-empty string.</param>
    public void AddWarnings(params string[] messages) => AddWarnings((IEnumerable<string>)messages);

    /// <summary>
    /// Adds an error message to the report and sets the <see cref="Status"/> to <see cref="ProcessingStatus.Failed"/>.
    /// </summary>
    /// <remarks>If the message is null, empty, or consists only of whitespace, it is ignored.</remarks>
    /// <param name="message">The error message to add.</param>
    public void AddError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _errors.Add(message);
        OnPropertyChanged(nameof(Errors));
        Status = ProcessingStatus.Failed;
    }

    /// <summary>
    /// Adds the specified error messages to the collection of errors.
    /// </summary>
    /// <remarks>If any messages are null, empty, or consist only of whitespace, they are ignored.</remarks>
    /// <param name="messages">An <see cref="IEnumerable{T}"/> of error messages to add.</param>
    public void AddErrors(IEnumerable<string> messages)
    {
        var anyAdded = false;

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message))
                continue;

            _errors.Add(message);
            anyAdded = true;
        }

        if (anyAdded)
        {
            OnPropertyChanged(nameof(Errors));
            Status = ProcessingStatus.Failed;
        }
    }

    /// <summary>
    /// Adds one or more error messages to the current collection of errors.
    /// </summary>
    /// <remarks>This method allows adding multiple error messages at once. If no messages are provided, the
    /// method does nothing.</remarks>
    /// <param name="messages">An array of error messages to add. Each message must be a non-null, non-empty string.</param>
    public void AddErrors(params string[] messages) => AddErrors((IEnumerable<string>)messages);

    /// <summary>
    /// Updates the current processing status to reflect the presence of warnings, if applicable.
    /// </summary>
    /// <remarks>If the current status is <see cref="ProcessingStatus.Succeeded"/> and there are warnings,  the status
    /// is updated to <see cref="ProcessingStatus.SucceededWithWarnings"/>.  This method does not perform any action if the
    /// status is not <see cref="ProcessingStatus.Succeeded"/>  or if there are no warnings.</remarks>
    private void UpdateStatusForIssues()
    {
        if (Status is ProcessingStatus.Succeeded && _warnings.Count > 0)
        {
            Status = ProcessingStatus.SucceededWithWarnings;
        }
    }

    /// <summary>
    /// Notify that a property changed.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
