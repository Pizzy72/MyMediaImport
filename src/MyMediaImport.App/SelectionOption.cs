namespace MyMediaImport.App;

public sealed record SelectionOption<T>(T Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
