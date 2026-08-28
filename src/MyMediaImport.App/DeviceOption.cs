namespace MyMediaImport.App;

public sealed record DeviceOption(string Id, string DisplayName, string? Manufacturer)
{
    public string DisplayLabel => string.IsNullOrWhiteSpace(Manufacturer)
        ? DisplayName
        : $"{DisplayName} - {Manufacturer}";

    public override string ToString() => DisplayLabel;
}
