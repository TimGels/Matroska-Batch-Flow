using System.Windows.Input;

namespace MatroskaBatchFlow.Uno.Presentation.Controls;

/// <summary>
/// Covers its parent while a long-running operation is active and shows the operation's progress.
/// </summary>
/// <remarks>
/// A Cancel button is shown when <see cref="CancelCommand"/> is set. It receives keyboard focus when the overlay appears,
/// and is disabled while <see cref="IsCancelling"/> is <see langword="true"/>.
/// </remarks>
public sealed partial class OperationOverlay : UserControl
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(OperationOverlay), new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(OperationOverlay), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(nameof(Detail), typeof(string), typeof(OperationOverlay), new PropertyMetadata(string.Empty, OnDetailChanged));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(OperationOverlay), new PropertyMetadata(true));

    public static readonly DependencyProperty CurrentProperty =
        DependencyProperty.Register(nameof(Current), typeof(int), typeof(OperationOverlay), new PropertyMetadata(0));

    public static readonly DependencyProperty TotalProperty =
        DependencyProperty.Register(nameof(Total), typeof(int), typeof(OperationOverlay), new PropertyMetadata(0));

    public static readonly DependencyProperty ProgressPercentProperty =
        DependencyProperty.Register(nameof(ProgressPercent), typeof(double), typeof(OperationOverlay), new PropertyMetadata(0d));

    public static readonly DependencyProperty BlocksInputProperty =
        DependencyProperty.Register(nameof(BlocksInput), typeof(bool), typeof(OperationOverlay), new PropertyMetadata(false));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(OperationOverlay), new PropertyMetadata(null, OnCancelCommandChanged));

    public static readonly DependencyProperty IsCancellingProperty =
        DependencyProperty.Register(nameof(IsCancelling), typeof(bool), typeof(OperationOverlay), new PropertyMetadata(false, OnIsCancellingChanged));

    public OperationOverlay()
    {
        this.InitializeComponent();
        UpdateDetailVisibility();
        UpdateCancelButton();
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets an optional line shown below the message. Hidden when empty.
    /// </summary>
    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public int Current
    {
        get => (int)GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public int Total
    {
        get => (int)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress bar value between 0 and 100.
    /// </summary>
    public double ProgressPercent
    {
        get => (double)GetValue(ProgressPercentProperty);
        set => SetValue(ProgressPercentProperty, value);
    }

    public bool BlocksInput
    {
        get => (bool)GetValue(BlocksInputProperty);
        set => SetValue(BlocksInputProperty, value);
    }

    /// <summary>
    /// Gets or sets the command executed by the Cancel button. The button is hidden when this is <see langword="null"/>.
    /// </summary>
    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets whether cancellation was requested. Disables the Cancel button.
    /// </summary>
    public bool IsCancelling
    {
        get => (bool)GetValue(IsCancellingProperty);
        set => SetValue(IsCancellingProperty, value);
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OperationOverlay overlay && e.NewValue is true)
        {
            overlay.FocusCancelButton();
        }
    }

    private static void OnDetailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as OperationOverlay)?.UpdateDetailVisibility();

    private static void OnCancelCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as OperationOverlay)?.UpdateCancelButton();

    private static void OnIsCancellingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as OperationOverlay)?.UpdateCancelButton();

    private void UpdateDetailVisibility()
    {
        DetailText.Visibility = string.IsNullOrEmpty(Detail) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateCancelButton()
    {
        CancelButton.Visibility = CancelCommand is null ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.IsEnabled = !IsCancelling;
    }

    /// <summary>
    /// Moves keyboard focus to the Cancel button once the overlay is shown, so focus does not stay on a control underneath.
    /// </summary>
    private void FocusCancelButton()
    {
        if (CancelCommand is null)
            return;

        DispatcherQueue.TryEnqueue(() =>
        {
            if (IsActive && CancelButton.Visibility == Visibility.Visible)
            {
                CancelButton.Focus(FocusState.Programmatic);
            }
        });
    }
}
