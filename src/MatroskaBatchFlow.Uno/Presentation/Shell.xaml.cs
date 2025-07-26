using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    private readonly SplashScreenLoadable _loadable = new();
    public Shell()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
        Splash.Source = _loadable;
    }
    public ContentControl ContentControl => Splash;

    public Frame RootFrame => ShellFrame;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loadable.Execute();
    }

    public class SplashScreenLoadable : ILoadable
    {
        public event EventHandler? IsExecutingChanged;

        private bool _isExecuting;
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    IsExecutingChanged?.Invoke(this, new());
                }
            }
        }

        public void Execute()
        {
            IsExecuting = true;
            //await Task.Delay(5000);
            WeakReferenceMessenger.Default.Register<ActivationCompletedMessage>(this, (r, m) => OnActivationCompleted());
        }

        private void OnActivationCompleted()
        {
            // Remove splash screen by setting IsExecuting to false
            IsExecuting = false;
        }
    }
}
