namespace MyMediaImport.Core;

public sealed record ImportRequest(
    MediaItem MediaItem,
    string TargetPath,
    ExistingFilePolicy ExistingFilePolicy);
