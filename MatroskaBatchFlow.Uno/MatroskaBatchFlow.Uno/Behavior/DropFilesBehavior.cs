using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;

namespace MatroskaBatchFlow.Uno.Behavior;

/// <summary>
/// Provides behavior for enabling drag-and-drop file operations on UI elements.
/// Special thanks to https://stackoverflow.com/a/75007093
/// </summary>
[Bindable]
public class DropFilesBehavior
{
    /// <summary>
    /// Attached property to enable or disable the drag-and-drop behavior.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(DropFilesBehavior), PropertyMetadata.Create(default(bool), OnIsEnabledChanged));

    /// <summary>
    /// Attached property to specify the target that will handle dropped files.
    /// </summary>
    public static readonly DependencyProperty FileDropTargetProperty = DependencyProperty.RegisterAttached(
        "FileDropTarget", typeof(IFilesDropped), typeof(DropFilesBehavior), null);

    /// <summary>
    /// Sets the value of the IsEnabled attached property.
    /// </summary>
    /// <param name="element">The dependency object to set the property on.</param>
    /// <param name="value">True to enable drag-and-drop; otherwise, false.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Gets the value of the IsEnabled attached property.
    /// </summary>
    /// <param name="element">The dependency object to get the property from.</param>
    /// <returns>True if drag-and-drop is enabled; otherwise, false.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>
    /// Sets the value of the FileDropTarget attached property.
    /// </summary>
    /// <param name="obj">The dependency object to set the property on.</param>
    /// <param name="value">The target that will handle dropped files.</param>
    public static void SetFileDropTarget(DependencyObject obj, IFilesDropped value)
    {
        obj.SetValue(FileDropTargetProperty, value);
    }

    /// <summary>
    /// Gets the value of the FileDropTarget attached property.
    /// </summary>
    /// <param name="obj">The dependency object to get the property from.</param>
    /// <returns>The target that will handle dropped files.</returns>
    public static IFilesDropped GetFileDropTarget(DependencyObject obj)
    {
        return (IFilesDropped)obj.GetValue(FileDropTargetProperty);
    }

    /// <summary>
    /// Handles changes to the IsEnabled attached property.
    /// </summary>
    /// <param name="d">The dependency object whose property changed.</param>
    /// <param name="e">Event arguments containing the old and new values.</param>
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var fe = d as FrameworkElement ?? throw new InvalidOperationException();

        if ((bool)e.NewValue)
        {
            fe.AllowDrop = true;
            fe.Drop += OnDrop;
            fe.DragOver += OnDragOver;
        }
        else
        {
            fe.AllowDrop = false;
            fe.Drop -= OnDrop;
            fe.DragOver -= OnDragOver;
        }
    }

    /// <summary>
    /// Handles the DragOver event to indicate that a drag-and-drop operation is allowed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments containing drag-and-drop data.</param>
    private static void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move; // or Link/Copy
        e.Handled = true;
    }

    /// <summary>
    /// Handles the Drop event to process dropped files.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments containing drag-and-drop data.</param>
    private static void OnDrop(object sender, DragEventArgs e)
    {
        var dobj = (DependencyObject)sender;
        var target = dobj.GetValue(FileDropTargetProperty);
        var filesDropped = target switch
        {
            IFilesDropped fd => fd,
            null => throw new InvalidOperationException("File drop target is not set."),
            _ => throw new InvalidOperationException($"Binding error, '{target.GetType().Name}' doesn't implement '{nameof(IFilesDropped)}'."),
        };

        if (filesDropped == null)
        {
            return;
        }

        var files = e.DataView.GetStorageItemsAsync().GetAwaiter().GetResult();
        if (files.Count == 0)
        {
            return;
        }

        filesDropped.OnFilesDropped(files.ToArray());
    }
}
