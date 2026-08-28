using MyMediaImport.Windows;

namespace MyMediaImport.App;

public sealed class PortableDeviceDiscoveryService(
    IPortableDeviceDiscovery portableDeviceDiscovery) : IDeviceDiscoveryService
{
    public async ValueTask<IReadOnlyList<DeviceOption>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PortableDeviceInfo> devices =
            await portableDeviceDiscovery.GetDevicesAsync(cancellationToken);

        return devices
            .Select(device => new DeviceOption(
                device.Id,
                device.DisplayName,
                device.Manufacturer))
            .ToArray();
    }
}
