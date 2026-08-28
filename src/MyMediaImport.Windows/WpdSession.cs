using System.Runtime.InteropServices;

namespace MyMediaImport.Windows;

internal sealed class WpdSession : IDisposable
{
    private object? _deviceObject;
    private object? _clientInfoObject;
    private object? _metadataKeysObject;
    private bool _opened;

    private WpdSession()
    {
    }

    internal WpdInterop.IPortableDeviceContent Content { get; private set; } = null!;
    internal WpdInterop.IPortableDeviceProperties Properties { get; private set; } = null!;
    internal WpdInterop.IPortableDeviceResources Resources { get; private set; } = null!;
    internal WpdInterop.IPortableDeviceKeyCollection MetadataKeys { get; private set; } = null!;

    internal static WpdSession Open(string deviceId)
    {
        WpdSession session = new();
        try
        {
            session.OpenCore(deviceId);
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    private void OpenCore(string deviceId)
    {
        Type deviceType = Type.GetTypeFromCLSID(WpdInterop.PortableDeviceClsid)
            ?? throw new PlatformNotSupportedException("WPD is not available on this system.");
        _deviceObject = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("The WPD device object could not be created.");

        Type valuesType = Type.GetTypeFromCLSID(WpdInterop.PortableDeviceValuesClsid)
            ?? throw new PlatformNotSupportedException("WPD values are not available on this system.");
        _clientInfoObject = Activator.CreateInstance(valuesType)
            ?? throw new InvalidOperationException("The WPD client information could not be created.");

        WpdInterop.IPortableDevice device = (WpdInterop.IPortableDevice)_deviceObject;
        device.Open(deviceId, (WpdInterop.IPortableDeviceValues)_clientInfoObject);
        _opened = true;
        device.Content(out WpdInterop.IPortableDeviceContent? content);
        Content = content;
        Content.Properties(out WpdInterop.IPortableDeviceProperties? properties);
        Properties = properties;
        Content.Transfer(out WpdInterop.IPortableDeviceResources? resources);
        Resources = resources;

        Type keysType = Type.GetTypeFromCLSID(WpdInterop.PortableDeviceKeyCollectionClsid)
            ?? throw new PlatformNotSupportedException("WPD property keys are not available.");
        _metadataKeysObject = Activator.CreateInstance(keysType)
            ?? throw new InvalidOperationException("The WPD property-key collection could not be created.");
        MetadataKeys = (WpdInterop.IPortableDeviceKeyCollection)_metadataKeysObject;
        foreach (WpdInterop.PropertyKey metadataProperty in WpdMediaKeys.MetadataProperties)
        {
            WpdInterop.PropertyKey key = metadataProperty;
            MetadataKeys.Add(ref key);
        }
    }

    public void Dispose()
    {
        ReleaseComObject(_metadataKeysObject);
        _metadataKeysObject = null;
        ReleaseComObject(Resources);
        Resources = null!;
        ReleaseComObject(Properties);
        Properties = null!;
        ReleaseComObject(Content);
        Content = null!;

        if (_deviceObject is WpdInterop.IPortableDevice device && _opened)
        {
            try
            {
                device.Close();
            }
            catch (COMException)
            {
                // Continue releasing resources when a device was disconnected.
            }

            _opened = false;
        }

        ReleaseComObject(_deviceObject);
        _deviceObject = null;
        ReleaseComObject(_clientInfoObject);
        _clientInfoObject = null;
    }

    internal static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
