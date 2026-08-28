namespace MyMediaImport.App;

public interface IDeviceDiscoveryService
{
    ValueTask<IReadOnlyList<DeviceOption>> GetDevicesAsync(
        CancellationToken cancellationToken = default);
}
