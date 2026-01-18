using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Activation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Services;

public partial class ActivationService(
    ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
    IEnumerable<IActivationHandler> activationHandlers,
    ILogger<ActivationService> logger) : IActivationService
{
    public async Task ActivateAsync(object activationArgs)
    {
        // Ensure that the MainWindow is initialized before activation.
        if (App.MainWindow is null)
            throw new InvalidOperationException("MainWindow must be initialized before activation.");

        LogActivationStarting();

        // Set the MainWindow as the active window.
        App.MainWindow.Activate();

        // Wait for the Shell to be loaded (visual tree ready).
        await WaitForShellLoaded();
        LogShellLoaded();

        // Short delay to increase the odds that the splash screen will become visible.
        await Task.Delay(500);

        // Perform logic that needs to run before splash screen removal.
        await InitializeAsync();
        LogInitializationCompleted();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);
        LogActivationHandlersCompleted();

        // Notify that activation is completed (for splash screen removal).
        WeakReferenceMessenger.Default.Send(new ActivationCompletedMessage());

        // Execute tasks after activation.
        await StartupAsync();
        LogActivationCompleted();
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

    /// <summary>
    /// Asynchronously waits for the Shell component to be fully loaded.
    /// </summary>
    /// <remarks>This method returns a task that completes when the Shell component's Loaded event is
    /// triggered. It assumes that the MainWindow.Content is of type Shell; otherwise, an exception is thrown.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task completes when the Shell component is loaded.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MainWindow.Content is not a Shell instance.</exception>
    private static Task WaitForShellLoaded()
    {
        var tcs = new TaskCompletionSource();
        if (App.MainWindow?.Content is Shell shell)
        {
            shell.Loaded += (s, e) => tcs.SetResult();
        } else
        {
            throw new InvalidOperationException("MainWindow.Content must be a Shell at this point.");
        }
        return tcs.Task;
    }

    private static async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    private static async Task StartupAsync()
    {
        await Task.CompletedTask;
    }
}
