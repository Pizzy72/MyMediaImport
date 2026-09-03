namespace MyMediaImport.Windows;

internal static class WpdMediaKeys
{
    private static readonly Guid ObjectProperties =
        new("EF6B490D-5CD8-437A-AFFC-DA8B60EE4A3C");

    internal static readonly WpdInterop.PropertyKey ObjectId = new(ObjectProperties, 2);
    internal static readonly WpdInterop.PropertyKey ParentId = new(ObjectProperties, 3);
    internal static readonly WpdInterop.PropertyKey Name = new(ObjectProperties, 4);
    internal static readonly WpdInterop.PropertyKey Format = new(ObjectProperties, 6);
    internal static readonly WpdInterop.PropertyKey ContentType = new(ObjectProperties, 7);
    internal static readonly WpdInterop.PropertyKey Size = new(ObjectProperties, 11);
    internal static readonly WpdInterop.PropertyKey OriginalFileName = new(ObjectProperties, 12);
    internal static readonly WpdInterop.PropertyKey DateCreated = new(ObjectProperties, 18);
    internal static readonly WpdInterop.PropertyKey DateModified = new(ObjectProperties, 19);

    internal static readonly WpdInterop.PropertyKey DefaultResource =
        new(new Guid("E81E79BE-34F0-41BF-B53F-F1A06AE87842"), 0);
    internal static readonly WpdInterop.PropertyKey ThumbnailResource =
        new(new Guid("C7C407BA-98FA-46B5-9960-23FEC124CFDE"), 0);

    internal static readonly Guid FolderContentType =
        new("27E2E392-A111-48E0-AB0C-E17705A05F85");
    internal static readonly Guid FunctionalObjectContentType =
        new("99ED0160-17FF-4C44-9D98-1D7A6F941921");

    internal static IReadOnlyList<WpdInterop.PropertyKey> MetadataProperties { get; } =
    [
        ObjectId,
        ParentId,
        Name,
        OriginalFileName,
        ContentType,
        Format,
        Size,
        DateCreated,
        DateModified
    ];
}
