namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// ViewModel for the configuration error window.
/// </summary>
public sealed partial class ConfigurationErrorViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Gets or sets the detailed error information (e.g., stack trace).
    /// </summary>
    [ObservableProperty]
    private string _errorDetails = string.Empty;

    /// <summary>
    /// Gets whether there are details to show.
    /// </summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(ErrorDetails);

    /// <summary>
    /// Gets the command to close the window and exit the application.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Environment.Exit(1);
    }

    partial void OnErrorDetailsChanged(string value)
    {
        OnPropertyChanged(nameof(HasDetails));
    }
}
