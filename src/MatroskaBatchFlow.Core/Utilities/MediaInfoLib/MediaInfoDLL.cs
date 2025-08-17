/*  Copyright (c) MediaArea.net SARL. All Rights Reserved.
 *
 *  Use of this source code is governed by a BSD-style license that can
 *  be found in the License.html file in the root of the source tree.
 */

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Microsoft Visual C# wrapper for MediaInfo Library
// See MediaInfo.h for help
//
// To make it working, you must put MediaInfo.Dll
// in the executable folder
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#pragma warning disable 1591 // Disable XML documentation warnings

namespace MatroskaBatchFlow.Core.Utilities.MediaInfoLib;

public enum StreamKind
{
    General,
    Video,
    Audio,
    Text,
    Other,
    Image,
    Menu,
}

public enum InfoKind
{
    Name,
    Text,
    Measure,
    Options,
    NameText,
    MeasureText,
    Info,
    HowTo
}

public enum InfoOptions
{
    ShowInInform,
    Support,
    ShowInSupported,
    TypeOfValue
}

[Flags]
public enum InfoFileOptions
{
    FileOption_Nothing = 0x00,
    FileOption_NoRecursive = 0x01,
    FileOption_CloseAll = 0x02,
    FileOption_Max = 0x04
};

[Flags]
public enum Status
{
    None = 0x00,
    Accepted = 0x01,
    Filled = 0x02,
    Updated = 0x04,
    Finalized = 0x08,
}

[ExcludeFromCodeCoverage(Justification = "MediaInfo DLL is a native wrapper")]
public class MediaInfo
{
    static MediaInfo()
    {
        _ = MediaInfoNativeLoader.EnsureLoaded;
    }

    //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_New();
    [DllImport("mediainfo")]
    private static extern void MediaInfo_Delete(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Open(IntPtr Handle, IntPtr FileName);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Open_Buffer_Init(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Open(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Open_Buffer_Continue(IntPtr Handle, IntPtr Buffer, IntPtr Buffer_Size);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr Handle, Int64 File_Size, byte[] Buffer, IntPtr Buffer_Size);
    [DllImport("mediainfo")]
    private static extern Int64 MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern Int64 MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern void MediaInfo_Close(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Inform(IntPtr Handle, IntPtr Reserved);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoA_Option(IntPtr Handle, IntPtr Option, IntPtr Value);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_State_Get(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

    //MediaInfo class
    public MediaInfo()
    {
        try
        {
            Handle = MediaInfo_New();
        }
        catch
        {
            Handle = 0;
        }
        if (Environment.OSVersion.ToString().IndexOf("Windows") == -1)
            MustUseAnsi = true;
        else
            MustUseAnsi = false;
    }
    ~MediaInfo() { if (Handle == 0) return; MediaInfo_Delete(Handle); }
    public int Open(string FileName)
    {
        if (Handle == 0)
            return 0;
        if (MustUseAnsi)
        {
            nint FileName_Ptr = Marshal.StringToHGlobalAnsi(FileName);
            int ToReturn = (int)MediaInfoA_Open(Handle, FileName_Ptr);
            Marshal.FreeHGlobal(FileName_Ptr);
            return ToReturn;
        }
        else
            return (int)MediaInfo_Open(Handle, FileName);
    }
    public int Open_Buffer_Init(long File_Size, long File_Offset)
    {
        if (Handle == 0) return 0; return (int)MediaInfo_Open_Buffer_Init(Handle, File_Size, File_Offset);
    }
    public int Open_Buffer_Continue(nint Buffer, nint Buffer_Size)
    {
        if (Handle == 0) return 0; return (int)MediaInfo_Open_Buffer_Continue(Handle, Buffer, Buffer_Size);
    }
    public long Open_Buffer_Continue_GoTo_Get()
    {
        if (Handle == 0) return 0; return MediaInfo_Open_Buffer_Continue_GoTo_Get(Handle);
    }
    public int Open_Buffer_Finalize()
    {
        if (Handle == 0) return 0; return (int)MediaInfo_Open_Buffer_Finalize(Handle);
    }
    public void Close() { if (Handle == 0) return; MediaInfo_Close(Handle); }
    public string Inform()
    {
        if (Handle == 0)
            return "Unable to load MediaInfo library";
        if (MustUseAnsi)
            return Marshal.PtrToStringAnsi(MediaInfoA_Inform(Handle, 0));
        else
            return Marshal.PtrToStringUni(MediaInfo_Inform(Handle, 0));
    }
    public string Get(StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch)
    {
        if (Handle == 0)
            return "Unable to load MediaInfo library";
        if (MustUseAnsi)
        {
            nint Parameter_Ptr = Marshal.StringToHGlobalAnsi(Parameter);
            string ToReturn = Marshal.PtrToStringAnsi(MediaInfoA_Get(Handle, (nint)StreamKind, StreamNumber, Parameter_Ptr, (nint)KindOfInfo, (nint)KindOfSearch));
            Marshal.FreeHGlobal(Parameter_Ptr);
            return ToReturn;
        }
        else
            return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (nint)StreamKind, StreamNumber, Parameter, (nint)KindOfInfo, (nint)KindOfSearch));
    }
    public string Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo)
    {
        if (Handle == 0)
            return "Unable to load MediaInfo library";
        if (MustUseAnsi)
            return Marshal.PtrToStringAnsi(MediaInfoA_GetI(Handle, (nint)StreamKind, StreamNumber, Parameter, (nint)KindOfInfo));
        else
            return Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (nint)StreamKind, StreamNumber, Parameter, (nint)KindOfInfo));
    }
    public string Option(string Option, string Value)
    {
        if (Handle == 0)
            return "Unable to load MediaInfo library";
        if (MustUseAnsi)
        {
            nint Option_Ptr = Marshal.StringToHGlobalAnsi(Option);
            nint Value_Ptr = Marshal.StringToHGlobalAnsi(Value);
            string ToReturn = Marshal.PtrToStringAnsi(MediaInfoA_Option(Handle, Option_Ptr, Value_Ptr));
            Marshal.FreeHGlobal(Option_Ptr);
            Marshal.FreeHGlobal(Value_Ptr);
            return ToReturn;
        }
        else
            return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value));
    }
    public int State_Get() { if (Handle == 0) return 0; return (int)MediaInfo_State_Get(Handle); }
    public int Count_Get(StreamKind StreamKind, int StreamNumber) { if (Handle == 0) return 0; return (int)MediaInfo_Count_Get(Handle, (nint)StreamKind, StreamNumber); }
    private nint Handle;
    private bool MustUseAnsi;

    //Default values, if you know how to set default values in C#, say me
    public string Get(StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo) { return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
    public string Get(StreamKind StreamKind, int StreamNumber, string Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
    public string Get(StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text); }
    public string Option(string Option_) { return Option(Option_, ""); }
    public int Count_Get(StreamKind StreamKind) { return Count_Get(StreamKind, -1); }
}

[ExcludeFromCodeCoverage(Justification = "MediaInfoList is a thin managed wrapper over native MediaInfo DLL functions.")]
public class MediaInfoList
{
    //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_New();
    [DllImport("mediainfo")]
    private static extern void MediaInfoList_Delete(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);
    [DllImport("mediainfo")]
    private static extern void MediaInfoList_Close(IntPtr Handle, IntPtr FilePos);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_Inform(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_GetI(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_State_Get(IntPtr Handle);
    [DllImport("mediainfo")]
    private static extern IntPtr MediaInfoList_Count_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);

    //MediaInfo class
    public MediaInfoList() { Handle = MediaInfoList_New(); }
    ~MediaInfoList() { MediaInfoList_Delete(Handle); }
    public int Open(string FileName, InfoFileOptions Options) { return (int)MediaInfoList_Open(Handle, FileName, (nint)Options); }
    public void Close(int FilePos) { MediaInfoList_Close(Handle, FilePos); }
    public string Inform(int FilePos) { return Marshal.PtrToStringUni(MediaInfoList_Inform(Handle, FilePos, 0)); }
    public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) { return Marshal.PtrToStringUni(MediaInfoList_Get(Handle, FilePos, (nint)StreamKind, StreamNumber, Parameter, (nint)KindOfInfo, (nint)KindOfSearch)); }
    public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) { return Marshal.PtrToStringUni(MediaInfoList_GetI(Handle, FilePos, (nint)StreamKind, StreamNumber, Parameter, (nint)KindOfInfo)); }
    public string Option(string Option, string Value) { return Marshal.PtrToStringUni(MediaInfoList_Option(Handle, Option, Value)); }
    public int State_Get() { return (int)MediaInfoList_State_Get(Handle); }
    public int Count_Get(int FilePos, StreamKind StreamKind, int StreamNumber) { return (int)MediaInfoList_Count_Get(Handle, FilePos, (nint)StreamKind, StreamNumber); }
    private nint Handle;

    //Default values, if you know how to set default values in C#, say me
    public void Open(string FileName) { Open(FileName, 0); }
    public void Close() { Close(-1); }
    public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter, InfoKind KindOfInfo) { return Get(FilePos, StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
    public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, string Parameter) { return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
    public string Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text); }
    public string Option(string Option_) { return Option(Option_, ""); }
    public int Count_Get(int FilePos, StreamKind StreamKind) { return Count_Get(FilePos, StreamKind, -1); }
}

//NameSpace
