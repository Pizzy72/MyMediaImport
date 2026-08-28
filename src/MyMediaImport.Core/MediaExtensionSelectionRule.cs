namespace MyMediaImport.Core;

public sealed class MediaExtensionSelectionRule : IMediaSelectionRule
{
    private readonly HashSet<string> _extensions;

    private MediaExtensionSelectionRule(IEnumerable<string> extensions)
    {
        _extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Extensions => _extensions;

    public static MediaExtensionSelectionRule Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                "--extension requires at least one file extension, for example JPG,HEIC,MOV.");
        }
        List<string> normalized = new();

        foreach (string entry in value.Split(',', StringSplitOptions.None))
        {
            string extension = entry.Trim();
            if (extension.Length == 0)
            {
                throw new FormatException(
                    "--extension contains an empty entry. Use values such as JPG,HEIC,MOV.");
            }

            if (extension.Any(character => !char.IsLetterOrDigit(character)))
            {
                throw new FormatException(
                    $"Invalid file extension '{entry}'. Extensions may contain only letters and digits, " +
                    "without a leading dot.");
            }

            normalized.Add(extension);
        }

        return new MediaExtensionSelectionRule(normalized);
    }

    public bool IsMatch(MediaItem mediaItem)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        string extension = Path.GetExtension(mediaItem.Name);
        return extension.Length > 1 && _extensions.Contains(extension[1..]);
    }
}
