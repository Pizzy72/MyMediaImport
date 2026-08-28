namespace MyMediaImport.Windows;

public interface IPortableDeviceDiscovery
{
    ValueTask<IReadOnlyList<PortableDeviceInfo>> GetDevicesAsync(
        CancellationToken cancellationToken = default);
}
