using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace MyMediaImport.Windows;

internal static class WpdInterop
{
    internal static readonly Guid PortableDeviceManagerClsid =
        new("0AF10CEC-2ECD-4B92-9581-34F6AE0637F3");
    internal static readonly Guid PortableDeviceClsid =
        new("728A21C5-3D9E-48D7-9810-864848F0F404");
    internal static readonly Guid PortableDeviceValuesClsid =
        new("0C15D503-D017-47CE-9016-7B3F978721CC");
    internal static readonly Guid PortableDeviceKeyCollectionClsid =
        new("DE2D022D-2480-43BE-97F0-D1FA2CF98F4F");

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct PropertyKey(Guid FormatId, uint PropertyId);

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct PropVariant
    {
        [FieldOffset(0)] internal ushort ValueType;
        [FieldOffset(8)] internal uint UInt32;
        [FieldOffset(8)] internal ulong UInt64;
        [FieldOffset(8)] internal double Double;
        [FieldOffset(8)] internal IntPtr Pointer;
    }

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropVariant propVariant);

    internal delegate int GetDeviceString(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder? value,
        ref uint characterCount);

    [ComImport]
    [Guid("A1567595-4C2F-4574-A6FA-ECEF917B9A40")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceManager
    {
        [PreserveSig]
        int GetDevices(IntPtr deviceIdPointers, ref uint deviceCount);

        [PreserveSig]
        int RefreshDeviceList();

        [PreserveSig]
        int GetDeviceFriendlyName(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder? friendlyName,
            ref uint characterCount);

        [PreserveSig]
        int GetDeviceDescription(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder? description,
            ref uint characterCount);

        [PreserveSig]
        int GetDeviceManufacturer(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder? manufacturer,
            ref uint characterCount);
    }

    [ComImport]
    [Guid("625E2DF8-6392-4CF0-9AD1-3CFA5F17775C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDevice
    {
        void Open(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IPortableDeviceValues clientInfo);

        void SendCommand(uint flags, IPortableDeviceValues parameters, out IPortableDeviceValues results);

        void Content(out IPortableDeviceContent content);

        void Capabilities([MarshalAs(UnmanagedType.Interface)] out object capabilities);

        void Cancel();

        void Close();
    }

    [ComImport]
    [Guid("6A96ED84-7C73-4480-9938-BF5AF477D426")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceContent
    {
        void EnumObjects(
            uint flags,
            [MarshalAs(UnmanagedType.LPWStr)] string parentObjectId,
            IPortableDeviceValues? filter,
            out IEnumPortableDeviceObjectIds objectIds);

        void Properties(out IPortableDeviceProperties properties);

        void Transfer(out IPortableDeviceResources resources);
    }

    [ComImport]
    [Guid("10ECE955-CF41-4728-BFA0-41EEDF1BBF19")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumPortableDeviceObjectIds
    {
        [PreserveSig]
        int Next(uint objectCount, IntPtr objectIdPointers, ref uint fetched);
        void Skip(uint objectCount);
        void Reset();
        void Clone(out IEnumPortableDeviceObjectIds clone);
        void Cancel();
    }

    [ComImport]
    [Guid("7F6D695C-03DF-4439-A809-59266BEEE3A6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceProperties
    {
        void GetSupportedProperties(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            out IPortableDeviceKeyCollection keys);

        void GetPropertyAttributes(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            ref PropertyKey key,
            out IPortableDeviceValues attributes);

        void GetValues(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            IPortableDeviceKeyCollection keys,
            out IPortableDeviceValues values);
    }

    [ComImport]
    [Guid("FD8878AC-D841-4D17-891C-E6829CDB6934")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceResources
    {
        void GetSupportedResources(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            out IPortableDeviceKeyCollection keys);

        void GetResourceAttributes(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            ref PropertyKey resourceKey,
            out IPortableDeviceValues attributes);

        void GetStream(
            [MarshalAs(UnmanagedType.LPWStr)] string objectId,
            ref PropertyKey resourceKey,
            uint mode,
            out uint optimalBufferSize,
            out IStream stream);
    }

    [ComImport]
    [Guid("DADA2357-E0AD-492E-98DB-DD61C53BA353")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceKeyCollection
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey key);
        void Add(ref PropertyKey key);
        void Clear();
        void RemoveAt(uint index);
    }

    [ComImport]
    [Guid("6848F6F2-3155-4F86-B6F5-263EEEAB3143")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceValues
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey key, out PropVariant value);
        void SetValue(ref PropertyKey key, ref PropVariant value);
        void GetValue(ref PropertyKey key, out PropVariant value);
    }
}
