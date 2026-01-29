using Microsoft.UI.Composition.SystemBackdrops;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Configuration error window.
/// </summary>
public sealed partial class ConfigurationErrorWindow : Window
{
    /// <summary>
    /// Gets the view model.
    /// </summary>
    public ConfigurationErrorViewModel ViewModel { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationErrorWindow"/> class.
    /// </summary>
    public ConfigurationErrorWindow()
    {
        this.InitializeComponent();

        // Apply Mica backdrop on Windows
#if WINDOWS10_0_19041_0_OR_GREATER
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
#endif
    }
}
