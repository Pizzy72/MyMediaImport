using MyMediaImport.Core;

namespace MyMediaImport.App;

public interface IMediaSourceFactory
{
    IMediaSource Create(DeviceOption device);
}
