using System.Reflection;

namespace MyMediaImport.App;

public static class ApplicationVersion
{
    public static string Current { get; } = ReadCurrent();

    public static string WindowTitle => $"MyMediaImport {Current}";

    private static string ReadCurrent()
    {
        Assembly assembly = typeof(ApplicationVersion).Assembly;
        string? informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        int buildMetadataSeparator = informationalVersion.IndexOf('+');
        return buildMetadataSeparator >= 0
            ? informationalVersion[..buildMetadataSeparator]
            : informationalVersion;
    }
}
