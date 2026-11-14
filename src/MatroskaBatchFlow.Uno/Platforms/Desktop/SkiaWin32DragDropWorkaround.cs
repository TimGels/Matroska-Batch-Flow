using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace MatroskaBatchFlow.Uno.Platforms.Desktop;

// Workaround for the drag-and-drop issue tracked at https://github.com/TimGels/Matroska-Batch-Flow/issues/101.
internal static partial class SkiaWin32DragDropWorkaround
{
    private const string DropTargetPropertyName = "OleDropTargetInterface";
    private const uint DataDirectionGet = 1;
    private const ushort ClipboardFormatHDrop = 15;
    private const uint DataAspectContent = 1;
    private const uint TymedHGlobal = 1;
    private const int HResultOk = 0;
    private const int HResultFalse = 1;
    private const int HResultPointer = unchecked((int)0x80004003);
    private const int HResultNoInterface = unchecked((int)0x80004002);
    private const int HResultUnexpected = unchecked((int)0x8000FFFF);
    private const int HResultFormatEtc = unchecked((int)0x80040064);

    private static readonly Guid DropTargetInterfaceId = new("00000122-0000-0000-C000-000000000046");
    private static readonly Guid DataObjectInterfaceId = new("0000010E-0000-0000-C000-000000000046");
    private static readonly Guid EnumFormatEtcInterfaceId = new("00000103-0000-0000-C000-000000000046");
    private static readonly Guid AsyncCapabilityInterfaceId = new("3D8B0590-F691-11D2-8EA9-006097DF5BD4");
    private static readonly ComWrappers ComWrappers = new StrategyBasedComWrappers();

    public static void Install(Microsoft.UI.Xaml.Window window)
    {
        if (global::Uno.UI.Xaml.WindowHelper.GetNativeWindow(window) is not global::Uno.UI.NativeElementHosting.Win32NativeWindow nativeWindow)
        {
            throw new InvalidOperationException("The Skia window does not expose a Win32 HWND.");
        }

        var hwnd = nativeWindow.Hwnd;
        var originalDropTarget = GetProp(hwnd, DropTargetPropertyName);
        if (originalDropTarget == 0)
        {
            throw new InvalidOperationException($"The OLE drop-target property '{DropTargetPropertyName}' was not found.");
        }

        Marshal.AddRef(originalDropTarget);
        try
        {
            var inner = (IWin32DropTarget)ComWrappers.GetOrCreateObjectForComInstance(originalDropTarget, CreateObjectFlags.None);
            var proxy = new DropTargetProxy(inner);
            var proxyUnknown = ComWrappers.GetOrCreateComInterfaceForObject(proxy, CreateComInterfaceFlags.None);
            try
            {
                var dropTarget = QueryInterface(proxyUnknown, DropTargetInterfaceId);

                try
                {
                    Marshal.ThrowExceptionForHR(RevokeDragDrop(hwnd));
                    var registerResult = RegisterDragDrop(hwnd, dropTarget);
                    if (registerResult < 0)
                    {
                        var registerException = Marshal.GetExceptionForHR(registerResult)
                            ?? new COMException("Failed to register the filtered drop target.", registerResult);
                        var restoreResult = RegisterDragDrop(hwnd, originalDropTarget);
                        if (restoreResult < 0)
                        {
                            var restoreException = Marshal.GetExceptionForHR(restoreResult)
                                ?? new COMException("Failed to restore Uno's original drop target.", restoreResult);
                            throw new AggregateException(
                                "Failed to register the filtered drop target and to restore Uno's original target.",
                                registerException,
                                restoreException);
                        }

                        throw registerException;
                    }
                }
                finally
                {
                    Marshal.Release(dropTarget);
                }
            }
            finally
            {
                Marshal.Release(proxyUnknown);
            }
        }
        finally
        {
            Marshal.Release(originalDropTarget);
        }
    }

    private static nint QueryInterface(nint unknown, Guid interfaceId)
    {
        var result = Marshal.QueryInterface(unknown, in interfaceId, out var interfacePointer);
        Marshal.ThrowExceptionForHR(result);
        return interfacePointer;
    }

    private static nint GetInterfacePointer(object value, Guid interfaceId)
    {
        var unknown = ComWrappers.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
        try
        {
            return QueryInterface(unknown, interfaceId);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "GetPropW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetProp(nint hwnd, string propertyName);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint RegisterClipboardFormatNative(string formatName);

    [LibraryImport("ole32.dll", EntryPoint = "RevokeDragDrop")]
    private static partial int RevokeDragDrop(nint hwnd);

    [LibraryImport("ole32.dll", EntryPoint = "RegisterDragDrop")]
    private static partial int RegisterDragDrop(nint hwnd, nint dropTarget);

    [GeneratedComInterface]
    [Guid("00000122-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IWin32DropTarget
    {
        [PreserveSig]
        int DragEnter(nint dataObject, uint keyState, PointL point, nint effect);

        [PreserveSig]
        int DragOver(uint keyState, PointL point, nint effect);

        [PreserveSig]
        int DragLeave();

        [PreserveSig]
        int Drop(nint dataObject, uint keyState, PointL point, nint effect);
    }

    [GeneratedComInterface]
    [Guid("0000010E-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IWin32DataObject
    {
        [PreserveSig]
        int GetData(nint format, nint medium);

        [PreserveSig]
        int GetDataHere(nint format, nint medium);

        [PreserveSig]
        int QueryGetData(nint format);

        [PreserveSig]
        int GetCanonicalFormatEtc(nint formatIn, nint formatOut);

        [PreserveSig]
        int SetData(nint format, nint medium, int release);

        [PreserveSig]
        int EnumFormatEtc(uint direction, nint enumerator);

        [PreserveSig]
        int DAdvise(nint format, uint advf, nint adviseSink, nint connection);

        [PreserveSig]
        int DUnadvise(uint connection);

        [PreserveSig]
        int EnumDAdvise(nint enumerator);
    }

    [GeneratedComInterface]
    [Guid("00000103-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IWin32EnumFormatEtc
    {
        [PreserveSig]
        int Next(uint count, nint formats, nint fetched);

        [PreserveSig]
        int Skip(uint count);

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int Clone(nint enumerator);
    }

    [GeneratedComInterface]
    [Guid("3D8B0590-F691-11D2-8EA9-006097DF5BD4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IWin32DataObjectAsyncCapability
    {
        [PreserveSig]
        int SetAsyncMode(int doOperationAsync);

        [PreserveSig]
        int GetAsyncMode(nint isOperationAsync);

        [PreserveSig]
        int StartOperation(nint bindContext);

        [PreserveSig]
        int InOperation(nint inAsyncOperation);

        [PreserveSig]
        int EndOperation(int result, nint bindContext, uint effects);
    }

    [GeneratedComClass]
    internal partial class DropTargetProxy(IWin32DropTarget inner) : IWin32DropTarget
    {
        public int DragEnter(nint dataObject, uint keyState, PointL point, nint effect)
            => ForwardDataObject(dataObject, filteredObject => inner.DragEnter(filteredObject, keyState, point, effect));

        public int DragOver(uint keyState, PointL point, nint effect)
            => inner.DragOver(keyState, point, effect);

        public int DragLeave()
            => inner.DragLeave();

        public int Drop(nint dataObject, uint keyState, PointL point, nint effect)
            => ForwardDataObject(dataObject, filteredObject => inner.Drop(filteredObject, keyState, point, effect));
    }

    [GeneratedComClass]
    internal partial class DataObjectProxy : IWin32DataObject, IWin32DataObjectAsyncCapability
    {
        private readonly IWin32DataObject _inner;
        private readonly IWin32DataObjectAsyncCapability? _asyncInner;
        private readonly ushort _fileGroupDescriptorFormat;
        private readonly ushort _fileContentsFormat;
        private readonly bool _hasHDrop;

        public DataObjectProxy(nint dataObject)
        {
            _inner = (IWin32DataObject)ComWrappers.GetOrCreateObjectForComInstance(dataObject, CreateObjectFlags.None);
            _asyncInner = TryGetAsyncCapability(dataObject);
            _fileGroupDescriptorFormat = RegisterClipboardFormat("FileGroupDescriptorW");
            _fileContentsFormat = RegisterClipboardFormat("FileContents");
            _hasHDrop = HasHDrop();
        }

        public int GetData(nint format, nint medium)
            => IsFiltered(format) ? HResultFormatEtc : _inner.GetData(format, medium);

        public int GetDataHere(nint format, nint medium)
            => IsFiltered(format) ? HResultFormatEtc : _inner.GetDataHere(format, medium);

        public int QueryGetData(nint format)
            => IsFiltered(format) ? HResultFormatEtc : _inner.QueryGetData(format);

        public int GetCanonicalFormatEtc(nint formatIn, nint formatOut)
            => IsFiltered(formatIn) ? HResultFormatEtc : _inner.GetCanonicalFormatEtc(formatIn, formatOut);

        public int SetData(nint format, nint medium, int release)
            => _inner.SetData(format, medium, release);

        public unsafe int EnumFormatEtc(uint direction, nint enumerator)
        {
            if (enumerator == 0)
            {
                return HResultPointer;
            }

            Marshal.WriteIntPtr(enumerator, 0);
            nint nativeEnumerator = 0;
            var result = _inner.EnumFormatEtc(direction, (nint)(&nativeEnumerator));
            if (result < 0)
            {
                return result;
            }

            if (nativeEnumerator == 0)
            {
                return HResultUnexpected;
            }

            if (direction != DataDirectionGet || !_hasHDrop)
            {
                Marshal.WriteIntPtr(enumerator, nativeEnumerator);
                return result;
            }

            try
            {
                var innerEnumerator = (IWin32EnumFormatEtc)ComWrappers.GetOrCreateObjectForComInstance(nativeEnumerator, CreateObjectFlags.None);
                var filteredEnumerator = new FormatEnumeratorProxy(innerEnumerator, _fileGroupDescriptorFormat, _fileContentsFormat);
                var filteredPointer = GetInterfacePointer(filteredEnumerator, EnumFormatEtcInterfaceId);
                Marshal.WriteIntPtr(enumerator, filteredPointer);
                return result;
            }
            finally
            {
                Marshal.Release(nativeEnumerator);
            }
        }

        public int DAdvise(nint format, uint advf, nint adviseSink, nint connection)
            => _inner.DAdvise(format, advf, adviseSink, connection);

        public int DUnadvise(uint connection)
            => _inner.DUnadvise(connection);

        public int EnumDAdvise(nint enumerator)
            => _inner.EnumDAdvise(enumerator);

        public int SetAsyncMode(int doOperationAsync)
            => _asyncInner?.SetAsyncMode(doOperationAsync) ?? HResultNoInterface;

        public int GetAsyncMode(nint isOperationAsync)
        {
            if (isOperationAsync == 0)
            {
                return HResultPointer;
            }

            if (_asyncInner is not null)
            {
                return _asyncInner.GetAsyncMode(isOperationAsync);
            }

            Marshal.WriteInt32(isOperationAsync, 0);
            return HResultOk;
        }

        public int StartOperation(nint bindContext)
            => _asyncInner?.StartOperation(bindContext) ?? HResultNoInterface;

        public int InOperation(nint inAsyncOperation)
            => _asyncInner?.InOperation(inAsyncOperation) ?? HResultNoInterface;

        public int EndOperation(int result, nint bindContext, uint effects)
            => _asyncInner?.EndOperation(result, bindContext, effects) ?? HResultNoInterface;

        private unsafe bool HasHDrop()
        {
            var format = new FormatEtc
            {
                ClipboardFormat = ClipboardFormatHDrop,
                Aspect = DataAspectContent,
                Index = -1,
                Tymed = TymedHGlobal,
            };

            return _inner.QueryGetData((nint)(&format)) >= 0;
        }

        private bool IsFiltered(nint format)
        {
            if (format == 0 || !_hasHDrop)
            {
                return false;
            }

            var clipboardFormat = unchecked((ushort)Marshal.ReadInt16(format));
            return clipboardFormat == _fileGroupDescriptorFormat || clipboardFormat == _fileContentsFormat;
        }
    }

    [GeneratedComClass]
    internal partial class FormatEnumeratorProxy(
        IWin32EnumFormatEtc inner,
        ushort fileGroupDescriptorFormat,
        ushort fileContentsFormat) : IWin32EnumFormatEtc
    {
        public unsafe int Next(uint count, nint formats, nint fetched)
        {
            if ((count != 0 && formats == 0) || (count > 1 && fetched == 0))
            {
                return HResultPointer;
            }

            var output = (FormatEtc*)formats;
            uint copied = 0;

            while (copied < count)
            {
                var current = default(FormatEtc);
                uint currentFetched = 0;
                var result = inner.Next(1, (nint)(&current), (nint)(&currentFetched));
                if (result < 0)
                {
                    return result;
                }

                if (currentFetched == 0)
                {
                    break;
                }

                if (current.ClipboardFormat == fileGroupDescriptorFormat || current.ClipboardFormat == fileContentsFormat)
                {
                    FreeTargetDevice(current.TargetDevice);
                    continue;
                }

                output[copied++] = current;
            }

            if (fetched != 0)
            {
                *(uint*)fetched = copied;
            }

            return copied == count ? HResultOk : HResultFalse;
        }

        public unsafe int Skip(uint count)
        {
            for (uint skipped = 0; skipped < count; skipped++)
            {
                var format = default(FormatEtc);
                uint fetched = 0;
                var result = Next(1, (nint)(&format), (nint)(&fetched));
                if (result < 0)
                {
                    return result;
                }

                if (fetched == 0)
                {
                    return HResultFalse;
                }

                FreeTargetDevice(format.TargetDevice);
            }

            return HResultOk;
        }

        public int Reset()
            => inner.Reset();

        public unsafe int Clone(nint enumerator)
        {
            if (enumerator == 0)
            {
                return HResultPointer;
            }

            Marshal.WriteIntPtr(enumerator, 0);
            nint nativeEnumerator = 0;
            var result = inner.Clone((nint)(&nativeEnumerator));
            if (result < 0)
            {
                return result;
            }

            if (nativeEnumerator == 0)
            {
                return HResultUnexpected;
            }

            try
            {
                var clone = (IWin32EnumFormatEtc)ComWrappers.GetOrCreateObjectForComInstance(nativeEnumerator, CreateObjectFlags.None);
                var filteredClone = new FormatEnumeratorProxy(clone, fileGroupDescriptorFormat, fileContentsFormat);
                var filteredPointer = GetInterfacePointer(filteredClone, EnumFormatEtcInterfaceId);
                Marshal.WriteIntPtr(enumerator, filteredPointer);
                return result;
            }
            finally
            {
                Marshal.Release(nativeEnumerator);
            }
        }

        private static void FreeTargetDevice(nint targetDevice)
        {
            if (targetDevice != 0)
            {
                Marshal.FreeCoTaskMem(targetDevice);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PointL
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FormatEtc
    {
        public ushort ClipboardFormat;
        public nint TargetDevice;
        public uint Aspect;
        public int Index;
        public uint Tymed;
    }

    private static ushort RegisterClipboardFormat(string formatName)
    {
        var format = RegisterClipboardFormatNative(formatName);
        if (format is 0 or > ushort.MaxValue)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to register the '{formatName}' clipboard format.");
        }

        return (ushort)format;
    }

    private static IWin32DataObjectAsyncCapability? TryGetAsyncCapability(nint dataObject)
    {
        var result = Marshal.QueryInterface(dataObject, in AsyncCapabilityInterfaceId, out var capability);
        if (result == HResultNoInterface)
        {
            return null;
        }

        Marshal.ThrowExceptionForHR(result);
        try
        {
            return (IWin32DataObjectAsyncCapability)ComWrappers.GetOrCreateObjectForComInstance(capability, CreateObjectFlags.None);
        }
        finally
        {
            Marshal.Release(capability);
        }
    }

    private static int ForwardDataObject(nint dataObject, Func<nint, int> forward)
    {
        if (dataObject == 0)
        {
            return HResultPointer;
        }

        var proxy = new DataObjectProxy(dataObject);
        var filteredObject = GetInterfacePointer(proxy, DataObjectInterfaceId);
        try
        {
            return forward(filteredObject);
        }
        finally
        {
            Marshal.Release(filteredObject);
        }
    }
}
