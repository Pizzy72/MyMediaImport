using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MyMediaImport.Windows.Diagnostics;

[SupportedOSPlatform("windows")]
public sealed class WpdDeviceInspector
{
    private const string RootObjectId = "DEVICE";

    public async Task InspectAsync(
        string deviceId,
        TextWriter output,
        WpdInspectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(output);
        options ??= new WpdInspectionOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.ObjectLimit, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaximumDepth);

        await Task.Run(
            () => Inspect(deviceId, output, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private static void Inspect(
        string deviceId,
        TextWriter output,
        WpdInspectionOptions options,
        CancellationToken cancellationToken)
    {
        object? deviceObject = null;
        object? clientInfoObject = null;
        WpdDiagnosticInterop.IPortableDeviceContent? content = null;
        WpdDiagnosticInterop.IPortableDeviceProperties? properties = null;
        WpdDiagnosticInterop.IPortableDeviceResources? resources = null;
        bool opened = false;

        try
        {
            Type deviceType = Type.GetTypeFromCLSID(WpdDiagnosticInterop.PortableDeviceClsid)
                ?? throw new PlatformNotSupportedException(
                    "Windows Portable Devices are not available on this system.");
            deviceObject = Activator.CreateInstance(deviceType)
                ?? throw new InvalidOperationException("The WPD device object could not be created.");
            WpdDiagnosticInterop.IPortableDevice device = (WpdDiagnosticInterop.IPortableDevice)deviceObject;
            Type valuesType = Type.GetTypeFromCLSID(WpdDiagnosticInterop.PortableDeviceValuesClsid)
                ?? throw new PlatformNotSupportedException(
                    "The WPD values collection is not available on this system.");
            clientInfoObject = Activator.CreateInstance(valuesType)
                ?? throw new InvalidOperationException("The WPD client information could not be created.");
            device.Open(deviceId, (WpdDiagnosticInterop.IPortableDeviceValues)clientInfoObject);
            opened = true;
            device.Content(out content);
            content.Properties(out properties);
            content.Transfer(out resources);

            output.WriteLine($"Device ID: {deviceId}");
            output.WriteLine($"Limits: {options.ObjectLimit} matching objects, depth {options.MaximumDepth}");
            if (options.Extensions.Count > 0)
            {
                output.WriteLine($"Extensions: {string.Join(", ", options.Extensions)}");
            }
            output.WriteLine();

            InspectionContext context = new(
                output, options, content, properties, resources, cancellationToken);
            InspectObject(RootObjectId, 0, context);

            if (context.MatchedCount == 0 && context.ExtensionFilter.Count > 0)
            {
                output.WriteLine("No objects matched the requested extensions.");
            }

            if (context.LimitReached)
            {
                output.WriteLine();
                output.WriteLine(
                    $"Output stopped after {options.ObjectLimit} matching objects. " +
                    "Increase --limit to inspect more.");
            }
        }
        finally
        {
            ReleaseComObject(resources);
            ReleaseComObject(properties);
            ReleaseComObject(content);

            if (deviceObject is WpdDiagnosticInterop.IPortableDevice device && opened)
            {
                try
                {
                    device.Close();
                }
                catch (COMException)
                {
                    // Resource release must continue even if the device was disconnected.
                }
            }

            ReleaseComObject(deviceObject);
            ReleaseComObject(clientInfoObject);
        }
    }

    private static void InspectObject(string objectId, int depth, InspectionContext context)
    {
        if (context.MatchedCount >= context.Options.ObjectLimit ||
            !context.VisitedObjectIds.Add(objectId))
        {
            context.LimitReached = context.MatchedCount >= context.Options.ObjectLimit;
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        WpdDiagnosticInterop.IPortableDeviceKeyCollection? supportedProperties = null;
        WpdDiagnosticInterop.IPortableDeviceValues? values = null;
        try
        {
            context.Properties.GetSupportedProperties(objectId, out supportedProperties);
            context.Properties.GetValues(objectId, supportedProperties, out values);
            string? originalFileName = GetValue(values, WpdDiagnosticKeys.OriginalFileName);
            if (MatchesExtension(originalFileName, context.ExtensionFilter))
            {
                context.MatchedCount++;
                WriteObject(objectId, depth, values, context);
            }
        }
        catch (COMException exception)
        {
            if (context.ExtensionFilter.Count == 0)
            {
                context.MatchedCount++;
                WriteIndent(context.Output, depth);
                context.Output.WriteLine($"Object ID: {objectId}");
                WriteIndent(context.Output, depth + 1);
                context.Output.WriteLine($"Property error: 0x{exception.HResult:X8} {exception.Message}");
            }
        }
        finally
        {
            ReleaseComObject(values);
            ReleaseComObject(supportedProperties);
        }

        if (context.MatchedCount >= context.Options.ObjectLimit)
        {
            context.LimitReached = true;
            return;
        }

        if (depth >= context.Options.MaximumDepth || context.LimitReached)
        {
            return;
        }

        foreach (string childId in EnumerateChildren(context.Content, objectId))
        {
            if (context.MatchedCount >= context.Options.ObjectLimit)
            {
                context.LimitReached = true;
                break;
            }

            InspectObject(childId, depth + 1, context);
        }
    }

    private static void WriteObject(
        string enumeratedObjectId,
        int depth,
        WpdDiagnosticInterop.IPortableDeviceValues values,
        InspectionContext context)
    {
        WriteIndent(context.Output, depth);
        context.Output.WriteLine($"Object ID: {GetValue(values, WpdDiagnosticKeys.ObjectId) ?? enumeratedObjectId}");
        WriteProperty(context.Output, depth, "Parent Object ID", GetValue(values, WpdDiagnosticKeys.ParentId));
        WriteProperty(context.Output, depth, "Name", GetValue(values, WpdDiagnosticKeys.Name));
        WriteProperty(context.Output, depth, "Original File Name", GetValue(values, WpdDiagnosticKeys.OriginalFileName));
        WriteProperty(context.Output, depth, "Content Type", GetValue(values, WpdDiagnosticKeys.ContentType));
        WriteProperty(context.Output, depth, "Format", GetValue(values, WpdDiagnosticKeys.Format));
        WriteProperty(context.Output, depth, "Size", GetValue(values, WpdDiagnosticKeys.Size));
        WriteProperty(context.Output, depth, "Date Created", GetValue(values, WpdDiagnosticKeys.DateCreated));
        WriteProperty(context.Output, depth, "Date Modified", GetValue(values, WpdDiagnosticKeys.DateModified));
        WriteProperty(context.Output, depth, "Date Authored", GetValue(values, WpdDiagnosticKeys.DateAuthored));
        WriteProperty(
            context.Output,
            depth,
            "Media Release Date",
            GetValue(values, WpdDiagnosticKeys.MediaReleaseDate));
        WriteProperty(
            context.Output,
            depth,
            "Media Last Accessed Time",
            GetValue(values, WpdDiagnosticKeys.MediaLastAccessedTime));
        WriteResources(enumeratedObjectId, depth, context);
        context.Output.WriteLine();
    }

    private static void WriteResources(
        string objectId,
        int depth,
        InspectionContext context)
    {
        WpdDiagnosticInterop.IPortableDeviceKeyCollection? keys = null;
        try
        {
            context.Resources.GetSupportedResources(objectId, out keys);
            keys.GetCount(out uint count);
            if (count == 0)
            {
                return;
            }

            WriteIndent(context.Output, depth + 1);
            context.Output.WriteLine("Resources:");

            for (uint index = 0; index < count; index++)
            {
                keys.GetAt(index, out WpdDiagnosticInterop.PropertyKey resourceKey);
                WriteIndent(context.Output, depth + 2);
                context.Output.WriteLine(
                    $"{WpdDiagnosticKeys.GetResourceName(resourceKey)} [{resourceKey}]");
                WriteResourceAttributes(objectId, resourceKey, depth, context);
            }
        }
        catch (COMException exception)
        {
            WriteIndent(context.Output, depth + 1);
            context.Output.WriteLine(
                $"Resource error: 0x{exception.HResult:X8} {exception.Message}");
        }
        finally
        {
            ReleaseComObject(keys);
        }
    }

    private static void WriteResourceAttributes(
        string objectId,
        WpdDiagnosticInterop.PropertyKey resourceKey,
        int depth,
        InspectionContext context)
    {
        WpdDiagnosticInterop.IPortableDeviceValues? attributes = null;
        try
        {
            context.Resources.GetResourceAttributes(objectId, ref resourceKey, out attributes);
            attributes.GetCount(out uint count);
            for (uint index = 0; index < count; index++)
            {
                attributes.GetAt(index, out WpdDiagnosticInterop.PropertyKey key, out WpdDiagnosticInterop.PropVariant value);
                try
                {
                    if (context.Options.VerboseResources ||
                        key == WpdDiagnosticKeys.ResourceTotalSize ||
                        key == WpdDiagnosticKeys.OptimalReadBufferSize ||
                        key == WpdDiagnosticKeys.ResourceFormat)
                    {
                        WriteIndent(context.Output, depth + 3);
                        context.Output.WriteLine(
                            $"{WpdDiagnosticKeys.GetAttributeName(key)}: {FormatValue(value)}");
                    }
                }
                finally
                {
                    WpdDiagnosticInterop.PropVariantClear(ref value);
                }
            }
        }
        catch (COMException exception)
        {
            WriteIndent(context.Output, depth + 3);
            context.Output.WriteLine(
                $"Attribute error: 0x{exception.HResult:X8} {exception.Message}");
        }
        finally
        {
            ReleaseComObject(attributes);
        }
    }

    private static IEnumerable<string> EnumerateChildren(
        WpdDiagnosticInterop.IPortableDeviceContent content,
        string parentObjectId)
    {
        WpdDiagnosticInterop.IEnumPortableDeviceObjectIds? enumerator = null;
        nint pointerArray = Marshal.AllocCoTaskMem(IntPtr.Size);

        try
        {
            content.EnumObjects(0, parentObjectId, null, out enumerator);
            while (true)
            {
                Marshal.WriteIntPtr(pointerArray, IntPtr.Zero);
                uint fetched = 0;
                int result = enumerator.Next(1, pointerArray, ref fetched);
                if (result < 0)
                {
                    Marshal.ThrowExceptionForHR(result);
                }

                if (fetched == 0)
                {
                    yield break;
                }

                nint objectIdPointer = Marshal.ReadIntPtr(pointerArray);
                try
                {
                    string? objectId = Marshal.PtrToStringUni(objectIdPointer);
                    if (!string.IsNullOrWhiteSpace(objectId))
                    {
                        yield return objectId;
                    }
                }
                finally
                {
                    if (objectIdPointer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(objectIdPointer);
                    }
                }
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointerArray);
            ReleaseComObject(enumerator);
        }
    }

    private static string? GetValue(
        WpdDiagnosticInterop.IPortableDeviceValues values,
        WpdDiagnosticInterop.PropertyKey key)
    {
        try
        {
            values.GetValue(ref key, out WpdDiagnosticInterop.PropVariant value);
            try
            {
                return FormatValue(value);
            }
            finally
            {
                WpdDiagnosticInterop.PropVariantClear(ref value);
            }
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static bool MatchesExtension(
        string? originalFileName,
        IReadOnlySet<string> extensionFilter)
    {
        if (extensionFilter.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return false;
        }

        string extension = Path.GetExtension(originalFileName).TrimStart('.');
        return extensionFilter.Contains(extension);
    }

    private static string FormatValue(WpdDiagnosticInterop.PropVariant value) =>
        value.ValueType switch
        {
            0 => "<empty>",
            1 => "<null>",
            2 => value.Int16.ToString(CultureInfo.InvariantCulture),
            3 or 22 => value.Int32.ToString(CultureInfo.InvariantCulture),
            4 => value.Float.ToString(CultureInfo.InvariantCulture),
            5 => value.Double.ToString(CultureInfo.InvariantCulture),
            7 => DateTime.FromOADate(value.Double).ToString("O", CultureInfo.InvariantCulture),
            8 => Marshal.PtrToStringBSTR(value.Pointer),
            10 => $"HRESULT 0x{value.Int32:X8}",
            11 => value.Int16 != 0 ? "true" : "false",
            16 => value.SignedByte.ToString(CultureInfo.InvariantCulture),
            17 => value.Byte.ToString(CultureInfo.InvariantCulture),
            18 => value.UInt16.ToString(CultureInfo.InvariantCulture),
            19 or 23 => value.UInt32.ToString(CultureInfo.InvariantCulture),
            20 => value.Int64.ToString(CultureInfo.InvariantCulture),
            21 => value.UInt64.ToString(CultureInfo.InvariantCulture),
            30 => Marshal.PtrToStringAnsi(value.Pointer) ?? "<null string>",
            31 => Marshal.PtrToStringUni(value.Pointer) ?? "<null string>",
            64 => DateTime.FromFileTimeUtc(value.Int64).ToString("O", CultureInfo.InvariantCulture),
            72 when value.Pointer != IntPtr.Zero => Marshal.PtrToStructure<Guid>(value.Pointer).ToString("D"),
            _ => $"VT={value.ValueType}, raw=0x{value.UInt64:X}"
        };

    private static void WriteProperty(TextWriter output, int depth, string name, string? value)
    {
        WriteIndent(output, depth + 1);
        output.WriteLine($"{name}: {value ?? "<not available>"}");
    }

    private static void WriteIndent(TextWriter output, int depth) =>
        output.Write(new string(' ', depth * 2));

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private sealed class InspectionContext(
        TextWriter output,
        WpdInspectionOptions options,
        WpdDiagnosticInterop.IPortableDeviceContent content,
        WpdDiagnosticInterop.IPortableDeviceProperties properties,
        WpdDiagnosticInterop.IPortableDeviceResources resources,
        CancellationToken cancellationToken)
    {
        internal TextWriter Output { get; } = output;
        internal WpdInspectionOptions Options { get; } = options;
        internal WpdDiagnosticInterop.IPortableDeviceContent Content { get; } = content;
        internal WpdDiagnosticInterop.IPortableDeviceProperties Properties { get; } = properties;
        internal WpdDiagnosticInterop.IPortableDeviceResources Resources { get; } = resources;
        internal CancellationToken CancellationToken { get; } = cancellationToken;
        internal HashSet<string> VisitedObjectIds { get; } = new(StringComparer.Ordinal);
        internal IReadOnlySet<string> ExtensionFilter { get; } = new HashSet<string>(
            options.Extensions.Select(extension => extension.Trim()),
            StringComparer.OrdinalIgnoreCase);
        internal int MatchedCount { get; set; }
        internal bool LimitReached { get; set; }
    }
}
