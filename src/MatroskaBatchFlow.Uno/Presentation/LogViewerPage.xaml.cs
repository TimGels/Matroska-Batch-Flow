using MatroskaBatchFlow.Uno.Models;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for viewing application log entries in real-time.
/// </summary>
public sealed partial class LogViewerPage : Page
{
    public LogViewerViewModel ViewModel { get; }

    public LogViewerPage()
    {
        this.InitializeComponent();

        ViewModel = App.GetService<LogViewerViewModel>();
        DataContext = ViewModel;

        // Register keyboard handler for Ctrl+A (select all) and Ctrl+C (copy) shortcuts
        this.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        HandleKeyboardShortcuts(e);
    }

    private void HandleKeyboardShortcuts(KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (!IsCtrlPressed())
        {
            return;
        }

        HandleCtrlKeyboardShortcut(e.Key, e);
    }

    private static bool IsCtrlPressed()
    {
        return InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
    }

    private void HandleCtrlKeyboardShortcut(VirtualKey key, KeyRoutedEventArgs e)
    {
        // Windows App SDK has native Ctrl+A handling, so we only need to implement it for other platforms.
#if !WINDOWS10_0_19041_0_OR_GREATER
        if (key == VirtualKey.A)
        {
            SelectAllItems();
            e.Handled = true;
        }
        else
#endif
        if (key == VirtualKey.C)
        {
            CopySelectedEntries();
            e.Handled = true;
        }
    }

#if !WINDOWS10_0_19041_0_OR_GREATER
    /// <summary>
    /// Selects all items in the ListView.
    /// Manual implementation for Skia/Desktop where ListViewBase.SelectAll() is not implemented.
    /// </summary>
    /// <remarks>
    /// WinAppSDK has native Ctrl+A handling and SelectAll() implementation, so this method is not needed there.
    /// </remarks>
    private void SelectAllItems()
    {
        LogListView.SelectedItems.Clear();
        foreach (var item in ViewModel.LogEntries)
        {
            LogListView.SelectedItems.Add(item);
        }
    }
#endif

    private void CopySelectedEntries()
    {
        if (LogListView.SelectedItems.Count == 0)
        {
            return;
        }

        var selectedEntries = LogListView.SelectedItems
            .OfType<LogEntry>()
            .OrderBy(entry => entry.Timestamp)
            .Select(entry => entry.FormattedMessage);

        var text = string.Join(Environment.NewLine, selectedEntries);

        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void CopySelectedItems_Click(object sender, RoutedEventArgs e)
    {
        CopySelectedEntries();
    }

    private void ClearAllItems_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearCommand.Execute(null);
    }
}
