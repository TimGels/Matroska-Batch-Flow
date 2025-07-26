using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Activation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Services;

public class ActivationService(
    ActivationHandler<LaunchActivatedEventArgs> defaultHandler, 
    IEnumerable<IActivationHandler> activationHandlers) : IActivationService
{
    private UIElement? _shell = null;

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null || App.MainWindow.Content is Shell)
        {
            _shell = App.GetService<MainPage>();
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Notify that activation is complete (for splash screen removal).
        WeakReferenceMessenger.Default.Send(new ActivationCompletedMessage());

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (defaultHandler.CanHandle(activationArgs))
        {
            await defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await Task.CompletedTask;
    }
}
