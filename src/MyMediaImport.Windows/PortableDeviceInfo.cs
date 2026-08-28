namespace MyMediaImport.Windows;

public sealed record PortableDeviceInfo(
    string Id,
    string DisplayName,
    string? Manufacturer,
    string? Description);
