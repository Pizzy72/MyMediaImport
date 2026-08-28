using System.Runtime.InteropServices;

namespace MyMediaImport.Windows.Diagnostics;

internal static class WpdDiagnosticInterop
{
    internal static readonly Guid PortableDeviceClsid =
        new("728A21C5-3D9E-48D7-9810-864848F0F404");
    internal static readonly Guid PortableDeviceValuesClsid =
        new("0C15D503-D017-47CE-9016-7B3F978721CC");

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct PropertyKey(Guid FormatId, uint PropertyId)
    {
        public override string ToString() => $"{{{FormatId:D}}}:{PropertyId}";
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct PropVariant
    {
        [FieldOffset(0)] internal ushort ValueType;
        [FieldOffset(8)] internal sbyte SignedByte;
        [FieldOffset(8)] internal byte Byte;
        [FieldOffset(8)] internal short Int16;
        [FieldOffset(8)] internal ushort UInt16;
        [FieldOffset(8)] internal int Int32;
        [FieldOffset(8)] internal uint UInt32;
        [FieldOffset(8)] internal long Int64;
        [FieldOffset(8)] internal ulong UInt64;
        [FieldOffset(8)] internal float Float;
        [FieldOffset(8)] internal double Double;
        [FieldOffset(8)] internal IntPtr Pointer;
    }

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropVariant propVariant);

    [ComImport]
    [Guid("625E2DF8-6392-4CF0-9AD1-3CFA5F17775C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDevice
    {
        void Open(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IPortableDeviceValues? clientInfo);

        void SendCommand(
            uint flags,
            IPortableDeviceValues parameters,
            out IPortableDeviceValues results);

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
            IPortableDeviceKeyCollection? keys,
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
    }

    [ComImport]
    [Guid("DADA2357-E0AD-492E-98DB-DD61C53BA353")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceKeyCollection
    {
        void GetCount(out uint count);

        void GetAt(uint index, out PropertyKey key);
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
