using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace MyMediaImport.Windows;

[SupportedOSPlatform("windows")]
public sealed class WpdPortableDeviceDiscovery : IPortableDeviceDiscovery
{
    public async ValueTask<IReadOnlyList<PortableDeviceInfo>> GetDevicesAsync(
        CancellationToken cancellationToken = default) =>
        await Task.Run(() => EnumerateDevices(cancellationToken), cancellationToken)
            .ConfigureAwait(false);

    private static IReadOnlyList<PortableDeviceInfo> EnumerateDevices(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        object? managerObject = null;
        try
        {
            Type managerType = Type.GetTypeFromCLSID(WpdInterop.PortableDeviceManagerClsid)
                ?? throw new PlatformNotSupportedException(
                    "Windows Portable Devices are not available on this system.");
            managerObject = Activator.CreateInstance(managerType)
                ?? throw new InvalidOperationException(
                    "The Windows Portable Device manager could not be created.");
            WpdInterop.IPortableDeviceManager manager = (WpdInterop.IPortableDeviceManager)managerObject;

            Marshal.ThrowExceptionForHR(manager.RefreshDeviceList());
            IReadOnlyList<string> deviceIds = GetDeviceIds(manager);
            List<PortableDeviceInfo> devices = new(deviceIds.Count);

            foreach (string deviceId in deviceIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? displayName = ReadString(manager.GetDeviceFriendlyName, deviceId);
                devices.Add(new PortableDeviceInfo(
                    deviceId,
                    string.IsNullOrWhiteSpace(displayName) ? deviceId : displayName,
                    NullIfWhiteSpace(ReadString(manager.GetDeviceManufacturer, deviceId)),
                    NullIfWhiteSpace(ReadString(manager.GetDeviceDescription, deviceId))));
            }

            return devices;
        }
        finally
        {
            if (managerObject is not null && Marshal.IsComObject(managerObject))
            {
                Marshal.FinalReleaseComObject(managerObject);
            }
        }
    }

    private static IReadOnlyList<string> GetDeviceIds(
        WpdInterop.IPortableDeviceManager manager)
    {
        uint deviceCount = 0;
        Marshal.ThrowExceptionForHR(manager.GetDevices(IntPtr.Zero, ref deviceCount));
        if (deviceCount == 0)
        {
            return [];
        }

        int pointerCount = checked((int)deviceCount);
        nint pointerArray = Marshal.AllocCoTaskMem(checked(pointerCount * IntPtr.Size));

        try
        {
            for (int index = 0; index < pointerCount; index++)
            {
                Marshal.WriteIntPtr(pointerArray, index * IntPtr.Size, IntPtr.Zero);
            }

            Marshal.ThrowExceptionForHR(manager.GetDevices(pointerArray, ref deviceCount));
            int returnedCount = Math.Min(checked((int)deviceCount), pointerCount);
            List<string> deviceIds = new(returnedCount);

            for (int index = 0; index < returnedCount; index++)
            {
                nint deviceIdPointer = Marshal.ReadIntPtr(pointerArray, index * IntPtr.Size);
                string? deviceId = Marshal.PtrToStringUni(deviceIdPointer);
                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceIds.Add(deviceId);
                }
            }

            return deviceIds;
        }
        finally
        {
            for (int index = 0; index < pointerCount; index++)
            {
                nint pointer = Marshal.ReadIntPtr(pointerArray, index * IntPtr.Size);
                if (pointer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pointer);
                }
            }

            Marshal.FreeCoTaskMem(pointerArray);
        }
    }

    private static string? ReadString(WpdInterop.GetDeviceString getValue, string deviceId)
    {
        try
        {
            uint characterCount = 0;
            int sizingResult = getValue(deviceId, null, ref characterCount);
            if (characterCount == 0)
            {
                if (sizingResult < 0)
                {
                    Marshal.ThrowExceptionForHR(sizingResult);
                }

                return null;
            }

            StringBuilder buffer = new(checked((int)characterCount));
            Marshal.ThrowExceptionForHR(getValue(deviceId, buffer, ref characterCount));
            return buffer.ToString();
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
