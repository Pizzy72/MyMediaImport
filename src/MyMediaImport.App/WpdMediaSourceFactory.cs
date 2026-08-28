using MyMediaImport.Core;
using MyMediaImport.Windows;

namespace MyMediaImport.App;

public sealed class WpdMediaSourceFactory : IMediaSourceFactory
{
    public IMediaSource Create(DeviceOption device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return new WpdMediaSource(device.Id);
    }
}
