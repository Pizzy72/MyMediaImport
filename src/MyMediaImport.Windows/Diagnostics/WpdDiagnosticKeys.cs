namespace MyMediaImport.Windows.Diagnostics;

internal static class WpdDiagnosticKeys
{
    private static readonly Guid ObjectProperties =
        new("EF6B490D-5CD8-437A-AFFC-DA8B60EE4A3C");
    private static readonly Guid ResourceAttributes =
        new("1EB6F604-9278-429F-93CC-5BB8C06656B6");

    internal static readonly WpdDiagnosticInterop.PropertyKey ObjectId =
        new(ObjectProperties, 2);
    internal static readonly WpdDiagnosticInterop.PropertyKey ParentId =
        new(ObjectProperties, 3);
    internal static readonly WpdDiagnosticInterop.PropertyKey Name =
        new(ObjectProperties, 4);
    internal static readonly WpdDiagnosticInterop.PropertyKey Format =
        new(ObjectProperties, 6);
    internal static readonly WpdDiagnosticInterop.PropertyKey ContentType =
        new(ObjectProperties, 7);
    internal static readonly WpdDiagnosticInterop.PropertyKey Size =
        new(ObjectProperties, 11);
    internal static readonly WpdDiagnosticInterop.PropertyKey OriginalFileName =
        new(ObjectProperties, 12);
    internal static readonly WpdDiagnosticInterop.PropertyKey DateCreated =
        new(ObjectProperties, 18);
    internal static readonly WpdDiagnosticInterop.PropertyKey DateModified =
        new(ObjectProperties, 19);
    internal static readonly WpdDiagnosticInterop.PropertyKey DateAuthored =
        new(ObjectProperties, 20);

    internal static readonly WpdDiagnosticInterop.PropertyKey MediaLastAccessedTime =
        new(new Guid("2ED8BA05-0AD3-42DC-B0D0-BC95AC396AC8"), 8);
    internal static readonly WpdDiagnosticInterop.PropertyKey MediaReleaseDate =
        new(new Guid("2ED8BA05-0AD3-42DC-B0D0-BC95AC396AC8"), 14);

    internal static readonly WpdDiagnosticInterop.PropertyKey DefaultResource =
        new(new Guid("E81E79BE-34F0-41BF-B53F-F1A06AE87842"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey ThumbnailResource =
        new(new Guid("C7C407BA-98FA-46B5-9960-23FEC124CFDE"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey IconResource =
        new(new Guid("F195FED8-AA28-4EE3-B153-E182DD5EDC39"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey AudioClipResource =
        new(new Guid("3BC13982-85B1-48E0-95A6-8D3AD06BE117"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey AlbumArtResource =
        new(new Guid("F02AA354-2300-4E2D-A1B9-3B6730F7FA21"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey GenericResource =
        new(new Guid("B9B9F515-BA70-4647-94DC-FA4925E95A07"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey ContactPhotoResource =
        new(new Guid("2C4D6803-80EA-4580-AF9A-5BE1A23EDDCB"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey VideoClipResource =
        new(new Guid("B566EE42-6368-4290-8662-70182FB79F20"), 0);
    internal static readonly WpdDiagnosticInterop.PropertyKey BrandingArtResource =
        new(new Guid("B633B1AE-6CAF-4A87-9589-22DED6DD5899"), 0);

    internal static readonly WpdDiagnosticInterop.PropertyKey ResourceTotalSize =
        new(ResourceAttributes, 2);
    internal static readonly WpdDiagnosticInterop.PropertyKey ResourceCanRead =
        new(ResourceAttributes, 3);
    internal static readonly WpdDiagnosticInterop.PropertyKey ResourceCanWrite =
        new(ResourceAttributes, 4);
    internal static readonly WpdDiagnosticInterop.PropertyKey ResourceCanDelete =
        new(ResourceAttributes, 5);
    internal static readonly WpdDiagnosticInterop.PropertyKey OptimalReadBufferSize =
        new(ResourceAttributes, 6);
    internal static readonly WpdDiagnosticInterop.PropertyKey OptimalWriteBufferSize =
        new(ResourceAttributes, 7);
    internal static readonly WpdDiagnosticInterop.PropertyKey ResourceFormat =
        new(ResourceAttributes, 8);

    internal static string GetResourceName(WpdDiagnosticInterop.PropertyKey key) =>
        key == DefaultResource ? "Default" :
        key == ThumbnailResource ? "Thumbnail" :
        key == IconResource ? "Icon" :
        key == AudioClipResource ? "Audio clip" :
        key == AlbumArtResource ? "Album art" :
        key == GenericResource ? "Generic" :
        key == ContactPhotoResource ? "Contact photo" :
        key == VideoClipResource ? "Video clip" :
        key == BrandingArtResource ? "Branding art" :
        key.ToString();

    internal static string GetAttributeName(WpdDiagnosticInterop.PropertyKey key) =>
        key == ResourceTotalSize ? "Total size" :
        key == ResourceCanRead ? "Can read" :
        key == ResourceCanWrite ? "Can write" :
        key == ResourceCanDelete ? "Can delete" :
        key == OptimalReadBufferSize ? "Optimal read buffer" :
        key == OptimalWriteBufferSize ? "Optimal write buffer" :
        key == ResourceFormat ? "Format" :
        key.ToString();
}
