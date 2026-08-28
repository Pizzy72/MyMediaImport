namespace MyMediaImport.Core;

public interface IMediaSelectionRule
{
    bool IsMatch(MediaItem mediaItem);
}
