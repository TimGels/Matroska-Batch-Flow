using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace MatroskaBatchFlow.Uno.Services.Interop;

/// <summary>
/// Values for <see cref="ITaskbarList3.SetProgressState"/> (TBPFLAG).
/// </summary>
internal enum TaskbarProgressState
{
    NoProgress = 0,
    Indeterminate = 0x1,
    Normal = 0x2,
    Error = 0x4,
    Paused = 0x8
}

/// <summary>
/// The Windows shell taskbar button interface.
/// </summary>
/// <remarks>
/// Methods are declared in vtable order, including those inherited from ITaskbarList and ITaskbarList2.
/// Members after <see cref="SetProgressState"/> are not needed and are omitted.
/// </remarks>
[GeneratedComInterface]
[Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
internal partial interface ITaskbarList3
{
    // ITaskbarList
    void HrInit();
    void AddTab(nint hwnd);
    void DeleteTab(nint hwnd);
    void ActivateTab(nint hwnd);
    void SetActiveAlt(nint hwnd);

    // ITaskbarList2
    void MarkFullscreenWindow(nint hwnd, int fullscreen);

    // ITaskbarList3
    void SetProgressValue(nint hwnd, ulong completed, ulong total);
    void SetProgressState(nint hwnd, TaskbarProgressState state);
}

/// <summary>
/// Creates the Windows shell taskbar list object.
/// </summary>
internal static partial class TaskbarListInterop
{
    private const uint ClassContextInProcessServer = 0x1;

    private static readonly Guid TaskbarListClassId = new("56fdf344-fd6d-11d0-958a-006097c9a090");
    private static readonly Guid TaskbarList3InterfaceId = typeof(ITaskbarList3).GUID;
    private static readonly StrategyBasedComWrappers ComWrappers = new();

    /// <summary>
    /// Creates and initializes an <see cref="ITaskbarList3"/> instance.
    /// </summary>
    /// <remarks>The object is apartment-threaded, so it must be created and used on the same STA thread.</remarks>
    /// <returns>The initialized taskbar list.</returns>
    /// <exception cref="COMException">Thrown when the object cannot be created.</exception>
    public static ITaskbarList3 Create()
    {
        Marshal.ThrowExceptionForHR(CoCreateInstance(TaskbarListClassId, 0, ClassContextInProcessServer, TaskbarList3InterfaceId, out var instance));

        try
        {
            var taskbarList = (ITaskbarList3)ComWrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.None);
            taskbarList.HrInit();
            return taskbarList;
        }
        finally
        {
            Marshal.Release(instance);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid classId, nint outer, uint classContext, in Guid interfaceId, out nint instance);
}
