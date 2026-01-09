using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Manages the state of validation notifications for display in the UI.
/// </summary>
public sealed class ValidationNotificationState : INotifyPropertyChanged, IDisposable
{
    private bool _hasErrors;
    private bool _hasWarnings;
    private bool _hasInfoMessages;
    private bool _hasNotifications;

    private int _errorCount;
    private int _warningCount;
    private int _infoCount;

    /// <summary>
    /// Gets the collection of all validation notification items.
    /// </summary>
    public ObservableCollection<ValidationNotificationItem> AllNotifications { get; } = new ObservableCollection<ValidationNotificationItem>();

    /// <summary>
    /// Gets validation errors (blocking).
    /// </summary>
    public IEnumerable<ValidationNotificationItem> Errors => AllNotifications.Where(n => n.IsError);

    /// <summary>
    /// Gets validation warnings (non-blocking).
    /// </summary>
    public IEnumerable<ValidationNotificationItem> Warnings => AllNotifications.Where(n => n.IsWarning);

    /// <summary>
    /// Gets validation info messages (non-blocking).
    /// </summary>
    public IEnumerable<ValidationNotificationItem> InfoMessages => AllNotifications.Where(n => n.IsInfo);

    /// <summary>
    /// Gets whether there are any notifications.
    /// </summary>
    public bool HasNotifications => _hasNotifications;

    /// <summary>
    /// Gets whether there are any errors.
    /// </summary>
    public bool HasErrors => _hasErrors;

    /// <summary>
    /// Gets whether there are any warnings.
    /// </summary>
    public bool HasWarnings => _hasWarnings;

    /// <summary>
    /// Gets whether there are any info messages.
    /// </summary>
    public bool HasInfoMessages => _hasInfoMessages;

    /// <summary>
    /// Gets the highest severity level among all notifications.
    /// </summary>
    public InfoBarSeverity HighestSeverity
    {
        get
        {
            if (HasErrors)
            {
                return InfoBarSeverity.Error;
            }

            if (HasWarnings)
            {
                return InfoBarSeverity.Warning;
            }

            if (HasInfoMessages)
            {
                return InfoBarSeverity.Informational;
            }

            return InfoBarSeverity.Success;
        }
    }

    /// <summary>
    /// Gets the summary title based on the highest severity.
    /// </summary>
    public string SummaryTitle
    {
        get
        {
            if (HasErrors)
            {
                return "Validation Errors";
            }

            if (HasWarnings)
            {
                return "Validation Warnings";
            }

            if (HasInfoMessages)
            {
                return "Validation Information";
            }

            return "Validation Passed";
        }
    }

    /// <summary>
    /// Gets a summary message showing counts of each notification type.
    /// </summary>
    public string SummaryMessage
    {
        get
        {
            var parts = new List<string>();

            if (_errorCount > 0)
            {
                parts.Add($"{_errorCount} error{(_errorCount == 1 ? "" : "s")}");
            }

            if (_warningCount > 0)
            {
                parts.Add($"{_warningCount} warning{(_warningCount == 1 ? "" : "s")}");
            }

            if (_infoCount > 0)
            {
                parts.Add($"{_infoCount} info message{(_infoCount == 1 ? "" : "s")}");
            }

            return parts.Count > 0
                ? string.Join(", ", parts)
                : "All files passed validation.";
        }
    }

    public ValidationNotificationState()
    {
        AllNotifications.CollectionChanged += OnCollectionChanged;
        RecomputeCache();
    }

    /// <summary>
    /// Clears all notifications.
    /// </summary>
    public void Clear()
    {
        AllNotifications.Clear();
    }

    /// <summary>
    /// Adds a notification item.
    /// </summary>
    public void AddNotification(ValidationNotificationItem item)
    {
        AllNotifications.Add(item);
    }

    /// <summary>
    /// Adds multiple notification items.
    /// </summary>
    public void AddNotifications(IEnumerable<ValidationNotificationItem> items)
    {
        foreach (var item in items)
        {
            AllNotifications.Add(item);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        AllNotifications.CollectionChanged -= OnCollectionChanged;
    }

    /// <summary>
    /// Handles collection change notifications and updates the internal cache accordingly.
    /// </summary>
    /// <param name="sender">The source of the collection change event. This is typically the collection that was modified.</param>
    /// <param name="eventArgs">An object that contains information about the change event, including the type of change and affected items.</param>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        RecomputeCache();
    }

    /// <summary>
    /// Recomputes cached counts and flags based on the current notifications.
    /// </summary>
    private void RecomputeCache()
    {
        _errorCount = AllNotifications.Count(n => n.IsError);
        _warningCount = AllNotifications.Count(n => n.IsWarning);
        _infoCount = AllNotifications.Count(n => n.IsInfo);

        _hasErrors = _errorCount > 0;
        _hasWarnings = _warningCount > 0;
        _hasInfoMessages = _infoCount > 0;
        _hasNotifications = AllNotifications.Count > 0;

        RaiseDerivedPropertyNotifications();
    }

    /// <summary>
    /// Raises property changed notifications for derived properties.
    /// </summary>
    private void RaiseDerivedPropertyNotifications()
    {
        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasInfoMessages));
        OnPropertyChanged(nameof(HighestSeverity));
        OnPropertyChanged(nameof(SummaryTitle));
        OnPropertyChanged(nameof(SummaryMessage));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify listeners that a property value has changed.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
