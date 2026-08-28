using MyMediaImport.Core;
using MyMediaImport.Cli;
using MyMediaImport.Windows;
using MyMediaImport.Windows.Diagnostics;
using System.Globalization;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    if (arguments.Length >= 1 &&
        arguments[0].Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        if (arguments.Length == 1)
        {
            WriteUsage();
            return CliExitCodes.Success;
        }

        if (arguments.Length == 2 &&
            CliHelp.TryGetCommandHelp(arguments[1], out string? commandHelp))
        {
            Console.Write(commandHelp);
            return CliExitCodes.Success;
        }

        if (arguments.Length == 3 &&
            arguments[2].Equals("syntax", StringComparison.OrdinalIgnoreCase) &&
            CliHelp.TryGetCommandSyntax(arguments[1], out string? syntaxHelp))
        {
            Console.Write(syntaxHelp);
            return CliExitCodes.Success;
        }

        Console.Error.WriteLine(arguments.Length == 2
            ? $"Unknown help topic '{arguments[1]}'."
            : "Use 'help <command>' or 'help <command> syntax'.");
        WriteUsage();
        return CliExitCodes.UsageError;
    }

    if (arguments.Length == 1 &&
        arguments[0].Equals("devices", StringComparison.OrdinalIgnoreCase))
    {
        return await ListDevicesAsync();
    }

    if (arguments.Length >= 1 &&
        arguments[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        return await InspectAsync(arguments[1..]);
    }

    if (arguments.Length >= 1 &&
        arguments[0].Equals("time-test", StringComparison.OrdinalIgnoreCase))
    {
        return RunTimeTest(arguments[1..]);
    }

    if (arguments.Length >= 1 &&
        arguments[0].Equals("list", StringComparison.OrdinalIgnoreCase))
    {
        return await ListMediaAsync(arguments[1..]);
    }

    if (arguments.Length >= 1 &&
        arguments[0].Equals("import-one", StringComparison.OrdinalIgnoreCase))
    {
        return await ImportOneAsync(arguments[1..]);
    }

    if (arguments.Length >= 1 &&
        arguments[0].Equals("import", StringComparison.OrdinalIgnoreCase))
    {
        return await ImportBatchAsync(arguments[1..]);
    }

    WriteUsage();
    return 2;
}

static async Task<int> ImportBatchAsync(string[] arguments)
{
    if (!TryParseImportArguments(arguments, out ImportArguments? parsed, out string? parseError))
    {
        Console.Error.WriteLine(parseError);
        WriteUsage();
        return CliExitCodes.UsageError;
    }

    CaptureTimeZoneSpec captureTimeZone;
    try
    {
        captureTimeZone = CaptureTimeZoneSpec.Parse(parsed!.TimeZone);
    }
    catch (Exception exception) when (exception is FormatException or ArgumentException)
    {
        return WriteError("Invalid capture timezone", exception);
    }

    using CancellationTokenSource cancellation = new();
    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    Console.CancelKeyPress += cancelHandler;

    try
    {
        IReadOnlyList<PortableDeviceInfo> devices = await DiscoverDevicesAsync();
        if (devices.Count == 0)
        {
            Console.Error.WriteLine("No portable devices found.");
            return CliExitCodes.Failure;
        }

        PortableDeviceInfo? device = SelectDeviceBySelection(
            devices, parsed.DeviceIndex, null, out string? selectionError);
        if (device is null)
        {
            Console.Error.WriteLine(selectionError);
            WriteDevices(devices);
            return CliExitCodes.UsageError;
        }

        WpdMediaSource source = new(device.Id);
        IMediaSelectionRule selectionRule = new LocalCaptureDateRangeSelectionRule(
            parsed.CaptureDateRange);
        if (parsed.ExtensionFilter is not null)
        {
            selectionRule = new AllOfMediaSelectionRule(
                selectionRule, parsed.ExtensionFilter);
        }
        List<MediaItem> selectedItems = new();
        await foreach (MediaItem mediaItem in source.GetMediaItemsAsync(cancellation.Token))
        {
            if (selectionRule.IsMatch(mediaItem))
            {
                selectedItems.Add(mediaItem);
            }
        }

        if (selectedItems.Count == 0)
        {
            Console.WriteLine("0 media files selected for the requested local capture-date range.");
            return CliExitCodes.Success;
        }

        LocalImportFileSystem fileSystem = new();
        ImportBatchPlanner batchPlanner = new(
            new ImportPlanner(fileSystem),
            new CaptureTimeZoneResolver());
        ImportPlan plan = await batchPlanner.CreatePlanAsync(
            selectedItems,
            source,
            captureTimeZone,
            new PathTemplate(parsed.TargetTemplate),
            parsed.ExistingFilePolicy,
            cancellation.Token);

        WriteImportPreview(plan, captureTimeZone, parsed.ExistingFilePolicy);

        SynchronousProgress<BatchImportProgress> progress = new(WriteImportProgress);
        BatchMediaImporter batchImporter = new(new MediaImporter(fileSystem));
        BatchImportResult result = await batchImporter.ImportAsync(
            source,
            plan,
            parsed.ExistingFilePolicy,
            progress,
            cancellation.Token);

        Console.WriteLine();
        Console.WriteLine($"Imported:  {result.ImportedCount}");
        Console.WriteLine($"Skipped:   {result.SkippedCount}");
        Console.WriteLine($"Failed:    {result.FailedCount}");
        return result.IsSuccess ? CliExitCodes.Success : CliExitCodes.Failure;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Import was cancelled.");
        return CliExitCodes.Failure;
    }
    catch (Exception exception)
    {
        return WriteError("Could not execute import", exception);
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

static bool TryParseImportArguments(
    string[] arguments,
    out ImportArguments? parsed,
    out string? error)
{
    int? deviceIndex = null;
    bool deviceIndexSpecified = false;
    LocalCaptureDatePreset? datePreset = null;
    int? lastDays = null;
    DateOnly? from = null;
    DateOnly? to = null;
    string? timezone = null;
    string? targetTemplate = null;
    bool timezoneSpecified = false;
    bool targetSpecified = false;
    ExistingFilePolicy policy = ExistingFilePolicy.Skip;
    int policyOptionCount = 0;
    MediaExtensionSelectionRule? extensionFilter = null;

    for (int index = 0; index < arguments.Length; index++)
    {
        string option = arguments[index];
        if (option.Equals("--today", StringComparison.OrdinalIgnoreCase) ||
            option.Equals("--yesterday", StringComparison.OrdinalIgnoreCase) ||
            option.Equals("--all", StringComparison.OrdinalIgnoreCase))
        {
            if (datePreset is not null)
            {
                parsed = null;
                error = "Use only one of --today, --yesterday, or --all.";
                return false;
            }

            datePreset = option.ToLowerInvariant() switch
            {
                "--today" => LocalCaptureDatePreset.Today,
                "--yesterday" => LocalCaptureDatePreset.Yesterday,
                _ => LocalCaptureDatePreset.All
            };
            continue;
        }

        if (option.Equals("--skip-existing", StringComparison.OrdinalIgnoreCase) ||
            option.Equals("--rename-existing", StringComparison.OrdinalIgnoreCase) ||
            option.Equals("--overwrite", StringComparison.OrdinalIgnoreCase))
        {
            policyOptionCount++;
            policy = option.ToLowerInvariant() switch
            {
                "--rename-existing" => ExistingFilePolicy.Rename,
                "--overwrite" => ExistingFilePolicy.Overwrite,
                _ => ExistingFilePolicy.Skip
            };
            continue;
        }

        if (index + 1 >= arguments.Length)
        {
            parsed = null;
            error = $"Option '{option}' requires a value.";
            return false;
        }

        string value = arguments[++index];
        if (option.Equals("--device-index", StringComparison.OrdinalIgnoreCase))
        {
            if (deviceIndexSpecified)
            {
                parsed = null;
                error = "--device-index may only be specified once.";
                return false;
            }

            if (!int.TryParse(value, out int selectedIndex) || selectedIndex < 1)
            {
                parsed = null;
                error = "--device-index must be a positive integer.";
                return false;
            }

            deviceIndex = selectedIndex;
            deviceIndexSpecified = true;
        }
        else if (option.Equals("--timezone", StringComparison.OrdinalIgnoreCase))
        {
            if (timezoneSpecified)
            {
                parsed = null;
                error = "--timezone may only be specified once.";
                return false;
            }

            timezone = value;
            timezoneSpecified = true;
        }
        else if (option.Equals("--target", StringComparison.OrdinalIgnoreCase))
        {
            if (targetSpecified)
            {
                parsed = null;
                error = "--target may only be specified once.";
                return false;
            }

            targetTemplate = value;
            targetSpecified = true;
        }
        else if (option.Equals("--extension", StringComparison.OrdinalIgnoreCase))
        {
            if (extensionFilter is not null)
            {
                parsed = null;
                error = "--extension may only be specified once.";
                return false;
            }

            if (!TryParseExtensionFilter(value, out extensionFilter, out error))
            {
                parsed = null;
                return false;
            }
        }
        else if (option.Equals("--last", StringComparison.OrdinalIgnoreCase))
        {
            if (value.Length < 2 ||
                value[^1] is not ('d' or 'D') ||
                !int.TryParse(
                    value[..^1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsedDays) ||
                parsedDays < 1)
            {
                parsed = null;
                error = "--last must use a positive number of calendar days, for example --last 3d.";
                return false;
            }

            if (lastDays is not null)
            {
                parsed = null;
                error = "--last may only be specified once.";
                return false;
            }

            lastDays = parsedDays;
        }
        else if (option.Equals("--from", StringComparison.OrdinalIgnoreCase) ||
                 option.Equals("--to", StringComparison.OrdinalIgnoreCase))
        {
            DateOnly parsedDate;
            try
            {
                parsedDate = LocalCaptureDateRange.ParseIsoDate(value);
            }
            catch (FormatException exception)
            {
                parsed = null;
                error = exception.Message;
                return false;
            }

            if (option.Equals("--from", StringComparison.OrdinalIgnoreCase))
            {
                if (from is not null)
                {
                    parsed = null;
                    error = "--from may only be specified once.";
                    return false;
                }

                from = parsedDate;
            }
            else
            {
                if (to is not null)
                {
                    parsed = null;
                    error = "--to may only be specified once.";
                    return false;
                }

                to = parsedDate;
            }
        }
        else
        {
            parsed = null;
            error = $"Unknown import option '{option}'.";
            return false;
        }
    }

    if (policyOptionCount > 1)
    {
        parsed = null;
        error = "Use at most one of --skip-existing, --rename-existing, or --overwrite.";
        return false;
    }

    if (deviceIndex is null ||
        string.IsNullOrWhiteSpace(timezone) || string.IsNullOrWhiteSpace(targetTemplate))
    {
        parsed = null;
        error = "Import requires --device-index, one time selection, --timezone, and --target.";
        return false;
    }

    LocalCaptureDateRange captureDateRange;
    try
    {
        captureDateRange = new LocalCaptureDateSelectionRequest(
                datePreset, lastDays, from, to)
            .Resolve(DateOnly.FromDateTime(DateTime.Now));
    }
    catch (ArgumentException exception)
    {
        parsed = null;
        error = exception.Message;
        return false;
    }

    parsed = new ImportArguments(
        deviceIndex.Value, timezone, targetTemplate, policy, captureDateRange, extensionFilter);
    error = null;
    return true;
}

static void WriteImportPreview(
    ImportPlan plan,
    CaptureTimeZoneSpec captureTimeZone,
    ExistingFilePolicy existingFilePolicy)
{
    const int previewLimit = 50;
    Console.WriteLine($"{plan.Items.Count} media files selected");
    Console.WriteLine($"Timezone: {captureTimeZone}");
    Console.WriteLine($"Existing files: {existingFilePolicy}");
    Console.WriteLine();

    foreach (ImportPlanItem? item in plan.Items.Take(previewLimit))
    {
        Console.WriteLine(item.MediaItem.Name);
        Console.WriteLine($"  -> {item.TargetPath}");
        if (item.Status is not ImportPlanStatus.Ready)
        {
            Console.WriteLine($"     {item.Status}: {item.Diagnostic}");
        }
    }

    if (plan.Items.Count > previewLimit)
    {
        Console.WriteLine($"... {plan.Items.Count - previewLimit} additional files omitted from preview");
    }

    long knownTotalSize = plan.Items
        .Where(item => item.MediaItem.Size is not null)
        .Sum(item => item.MediaItem.Size!.Value);
    int unknownSizeCount = plan.Items.Count(item => item.MediaItem.Size is null);
    Console.WriteLine();
    Console.WriteLine(
        $"Total: {FormatFileSize(knownTotalSize)}" +
        (unknownSizeCount > 0 ? $" plus {unknownSizeCount} file(s) of unknown size" : string.Empty));
    Console.WriteLine();
}

static void WriteImportProgress(BatchImportProgress progress)
{
    string label = progress.Result.Status switch
    {
        ImportResultStatus.Succeeded => "OK",
        ImportResultStatus.Skipped => "SKIPPED",
        ImportResultStatus.AlreadyImported => "ALREADY EXISTS (identical)",
        ImportResultStatus.Cancelled => "CANCELLED",
        _ => "FAILED"
    };
    Console.WriteLine(
        $"[{progress.CompletedCount}/{progress.TotalCount}] " +
        $"{progress.Result.MediaItem.Name} ... {label}");
    if (progress.Result.Status == ImportResultStatus.Failed &&
        progress.Result.Diagnostic is not null)
    {
        Console.WriteLine($"  {progress.Result.Diagnostic}");
    }
}

static async Task<int> ImportOneAsync(string[] arguments)
{
    if (!TryParseImportOneArguments(arguments, out ImportOneArguments? parsed, out string? parseError))
    {
        Console.Error.WriteLine(parseError);
        WriteUsage();
        return 2;
    }

    using CancellationTokenSource cancellation = new();
    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    Console.CancelKeyPress += cancelHandler;

    try
    {
        IReadOnlyList<PortableDeviceInfo> devices = await DiscoverDevicesAsync();
        if (devices.Count == 0)
        {
            Console.WriteLine("No portable devices found.");
            return 2;
        }

        PortableDeviceInfo? device = SelectDeviceBySelection(
            devices, parsed!.DeviceIndex, null, out string? selectionError);
        if (device is null)
        {
            Console.Error.WriteLine(selectionError);
            WriteDevices(devices);
            return 2;
        }

        WpdMediaSource source = new(device.Id);
        MediaItem? selectedItem = null;
        await foreach (MediaItem mediaItem in source.GetMediaItemsAsync(cancellation.Token))
        {
            if (mediaItem.Id.Equals(parsed.MediaItemId, StringComparison.Ordinal))
            {
                selectedItem = mediaItem;
                break;
            }
        }

        if (selectedItem is null)
        {
            Console.Error.WriteLine(
                $"No supported media item with ID '{parsed.MediaItemId}' was found on the selected device.");
            return 2;
        }

        MediaImporter importer = new(new LocalImportFileSystem());
        ImportResult result = await importer.ImportAsync(
            source,
            new ImportRequest(selectedItem, parsed.TargetPath, parsed.Policy),
            cancellation.Token);

        Console.WriteLine($"Source:        {selectedItem.Name} ({selectedItem.Id})");
        Console.WriteLine($"Target:        {result.TargetPath}");
        Console.WriteLine(
            $"Expected size: {result.ExpectedSize?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}");
        Console.WriteLine($"Transferred:   {result.TransferredBytes}");
        Console.WriteLine($"Result:        {result.Status}");
        if (result.Diagnostic is not null)
        {
            Console.WriteLine($"Diagnostic:    {result.Diagnostic}");
        }

        return result.Status is ImportResultStatus.Succeeded or
            ImportResultStatus.Skipped or
            ImportResultStatus.AlreadyImported ? 0 : 1;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Import was cancelled.");
        return 1;
    }
    catch (Exception exception)
    {
        return WriteError("Could not import media file", exception);
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

static bool TryParseImportOneArguments(
    string[] arguments,
    out ImportOneArguments? parsed,
    out string? error)
{
    int? deviceIndex = null;
    bool deviceIndexSpecified = false;
    string? mediaItemId = null;
    string? targetPath = null;
    bool targetSpecified = false;
    ExistingFilePolicy policy = ExistingFilePolicy.Skip;

    for (int index = 0; index < arguments.Length; index++)
    {
        string option = arguments[index];
        if (index + 1 >= arguments.Length)
        {
            parsed = null;
            error = $"Option '{option}' requires a value.";
            return false;
        }

        string value = arguments[++index];
        if (option.Equals("--device-index", StringComparison.OrdinalIgnoreCase))
        {
            if (deviceIndexSpecified)
            {
                parsed = null;
                error = "--device-index may only be specified once.";
                return false;
            }

            if (!int.TryParse(value, out int selectedIndex) || selectedIndex < 1)
            {
                parsed = null;
                error = "--device-index must be a positive integer.";
                return false;
            }

            deviceIndex = selectedIndex;
            deviceIndexSpecified = true;
        }
        else if (option.Equals("--id", StringComparison.OrdinalIgnoreCase))
        {
            mediaItemId = value;
        }
        else if (option.Equals("--target", StringComparison.OrdinalIgnoreCase))
        {
            if (targetSpecified)
            {
                parsed = null;
                error = "--target may only be specified once.";
                return false;
            }

            targetPath = value;
            targetSpecified = true;
        }
        else if (option.Equals("--policy", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ExistingFilePolicy>(value, ignoreCase: true, out policy) ||
                !Enum.IsDefined(policy))
            {
                parsed = null;
                error = "--policy must be skip, rename, or overwrite.";
                return false;
            }
        }
        else
        {
            parsed = null;
            error = $"Unknown import-one option '{option}'.";
            return false;
        }
    }

    if (deviceIndex is null)
    {
        parsed = null;
        error = "Missing required option '--device-index'.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(mediaItemId))
    {
        parsed = null;
        error = "Missing required option '--id'.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(targetPath))
    {
        parsed = null;
        error = "Missing required option '--target'.";
        return false;
    }

    parsed = new ImportOneArguments(deviceIndex.Value, mediaItemId, targetPath, policy);
    error = null;
    return true;
}

static async Task<int> ListMediaAsync(string[] arguments)
{
    if (!TryParseListArguments(arguments, out ListArguments? parsed, out string? parseError))
    {
        Console.Error.WriteLine(parseError);
        WriteUsage();
        return 2;
    }

    try
    {
        IReadOnlyList<PortableDeviceInfo> devices = await DiscoverDevicesAsync();
        if (devices.Count == 0)
        {
            Console.WriteLine("No portable devices found.");
            return 0;
        }

        PortableDeviceInfo? device = SelectDeviceBySelection(
            devices, parsed!.DeviceIndex, null, out string? selectionError);
        if (device is null)
        {
            Console.Error.WriteLine(selectionError);
            WriteDevices(devices);
            return 2;
        }

        WpdMediaSource source = new(device.Id);
        int count = 0;
        await foreach (MediaItem mediaItem in source.GetMediaItemsAsync())
        {
            if (parsed.ExtensionFilter is not null &&
                !parsed.ExtensionFilter.IsMatch(mediaItem))
            {
                continue;
            }

            Console.WriteLine(
                $"{mediaItem.Name,-24} {mediaItem.MediaKind,-7} " +
                $"{FormatFileSize(mediaItem.Size),10}  " +
                $"{FormatCaptureTime(mediaItem.CaptureTime)}" +
                (parsed.ShowId ? $"  [{mediaItem.Id}]" : string.Empty));
            count++;
            if (count >= parsed.Limit)
            {
                break;
            }
        }

        if (count == 0)
        {
            Console.WriteLine("No supported photo or video files found.");
        }

        return 0;
    }
    catch (Exception exception)
    {
        return WriteError("Could not list media files", exception);
    }
}

static bool TryParseListArguments(
    string[] arguments,
    out ListArguments? parsed,
    out string? error)
{
    int? deviceIndex = null;
    bool deviceIndexSpecified = false;
    int limit = 50;
    bool showId = false;
    MediaExtensionSelectionRule? extensionFilter = null;

    for (int index = 0; index < arguments.Length; index++)
    {
        string option = arguments[index];
        if (option.Equals("--show-id", StringComparison.OrdinalIgnoreCase))
        {
            showId = true;
            continue;
        }

        if (index + 1 >= arguments.Length)
        {
            parsed = null;
            error = $"Option '{option}' requires a value.";
            return false;
        }

        string value = arguments[++index];
        if (option.Equals("--device-index", StringComparison.OrdinalIgnoreCase))
        {
            if (deviceIndexSpecified)
            {
                parsed = null;
                error = "--device-index may only be specified once.";
                return false;
            }

            if (!int.TryParse(value, out int selectedIndex) || selectedIndex < 1)
            {
                parsed = null;
                error = "--device-index must be a positive integer.";
                return false;
            }

            deviceIndex = selectedIndex;
            deviceIndexSpecified = true;
        }
        else if (option.Equals("--limit", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(value, out limit) || limit < 1)
            {
                parsed = null;
                error = "--limit must be a positive integer.";
                return false;
            }
        }
        else if (option.Equals("--extension", StringComparison.OrdinalIgnoreCase))
        {
            if (extensionFilter is not null)
            {
                parsed = null;
                error = "--extension may only be specified once.";
                return false;
            }

            if (!TryParseExtensionFilter(value, out extensionFilter, out error))
            {
                parsed = null;
                return false;
            }
        }
        else
        {
            parsed = null;
            error = $"Unknown list option '{option}'.";
            return false;
        }
    }

    parsed = new ListArguments(deviceIndex, limit, showId, extensionFilter);
    error = null;
    return true;
}

static bool TryParseExtensionFilter(
    string value,
    out MediaExtensionSelectionRule? filter,
    out string? error)
{
    try
    {
        filter = MediaExtensionSelectionRule.Parse(value);
        error = null;
        return true;
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException)
    {
        filter = null;
        error = exception.Message;
        return false;
    }
}

static string FormatFileSize(long? size) =>
    size is null
        ? "unknown"
        : size >= 1024 * 1024
        ? $"{size / (1024d * 1024d):0.0} MB"
        : size >= 1024
            ? $"{size / 1024d:0.0} KB"
            : $"{size} B";

static string FormatCaptureTime(CaptureTimestamp? captureTime) =>
    captureTime is null
        ? "<capture time unavailable>"
        : captureTime.LocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

static int RunTimeTest(string[] arguments)
{
    if (!TryParseTimeTestArguments(arguments, out string? captureText, out string? timezoneText, out string? error))
    {
        Console.Error.WriteLine(error);
        WriteTimeTestUsage();
        return 2;
    }

    string[] supportedFormats = new[]
    {
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
    };
    if (!DateTime.TryParseExact(
        captureText,
        supportedFormats,
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out DateTime localCaptureTime))
    {
        Console.Error.WriteLine(
            $"Invalid capture timestamp '{captureText}'. Use a local timestamp such as 2026-08-23T14:53:20.");
        return 2;
    }

    try
    {
        CaptureTimeZoneSpec timeZoneSpec = CaptureTimeZoneSpec.Parse(timezoneText!);
        CaptureTimeZoneResolver resolver = new();
        DateTimeOffset resolved = resolver.Resolve(localCaptureTime, timeZoneSpec);

        Console.WriteLine($"Input local time:   {localCaptureTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Timezone:           {timeZoneSpec}");
        if (timeZoneSpec.Kind == CaptureTimeZoneKind.Local)
        {
            Console.WriteLine(
                $"Local timezone:      {resolver.LocalTimeZoneDisplayName} ({resolver.LocalTimeZoneId})");
        }

        Console.WriteLine($"Resolved timestamp: {resolved:yyyy-MM-dd'T'HH:mm:sszzz}");
        Console.WriteLine($"UTC:                {resolved.UtcDateTime:yyyy-MM-dd'T'HH:mm:ss'Z'}");
        return 0;
    }
    catch (Exception exception) when (
        exception is FormatException or ArgumentException or CaptureTimeResolutionException)
    {
        return WriteError("Could not resolve capture time", exception);
    }
}

static bool TryParseTimeTestArguments(
    string[] arguments,
    out string? capture,
    out string? timezone,
    out string? error)
{
    capture = null;
    timezone = null;

    for (int index = 0; index < arguments.Length; index++)
    {
        string option = arguments[index];
        if (!option.Equals("--capture", StringComparison.OrdinalIgnoreCase) &&
            !option.Equals("--timezone", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unknown time-test option '{option}'.";
            return false;
        }

        if (index + 1 >= arguments.Length)
        {
            error = $"Option '{option}' requires a value.";
            return false;
        }

        string value = arguments[++index];
        if (option.Equals("--capture", StringComparison.OrdinalIgnoreCase))
        {
            if (capture is not null)
            {
                error = "Option '--capture' may only be specified once.";
                return false;
            }

            capture = value;
        }
        else
        {
            if (timezone is not null)
            {
                error = "Option '--timezone' may only be specified once.";
                return false;
            }

            timezone = value;
        }
    }

    if (capture is null)
    {
        error = "Missing required option '--capture'.";
        return false;
    }

    if (timezone is null)
    {
        error = "Missing required option '--timezone'.";
        return false;
    }

    error = null;
    return true;
}

static async Task<int> ListDevicesAsync()
{
    try
    {
        IReadOnlyList<PortableDeviceInfo> devices = await DiscoverDevicesAsync();
        if (devices.Count == 0)
        {
            Console.WriteLine("No portable devices found.");
            return 0;
        }

        WriteDevices(devices);
        return 0;
    }
    catch (Exception exception)
    {
        return WriteError("Could not enumerate portable devices", exception);
    }
}

static async Task<int> InspectAsync(string[] arguments)
{
    if (!TryParseInspectionArguments(arguments, out InspectionArguments? parsed, out string? parseError))
    {
        Console.Error.WriteLine(parseError);
        WriteUsage();
        return 2;
    }

    try
    {
        IReadOnlyList<PortableDeviceInfo> devices = await DiscoverDevicesAsync();
        if (devices.Count == 0)
        {
            Console.WriteLine("No portable devices found.");
            return 0;
        }

        PortableDeviceInfo? device = SelectDevice(devices, parsed!, out string? selectionError);
        if (device is null)
        {
            Console.Error.WriteLine(selectionError);
            WriteDevices(devices);
            return 2;
        }

        Console.WriteLine($"Inspecting {device.DisplayName}");
        WpdDeviceInspector inspector = new();
        await inspector.InspectAsync(
            device.Id,
            Console.Out,
            new WpdInspectionOptions
            {
                ObjectLimit = parsed!.Limit,
                MaximumDepth = parsed.Depth,
                VerboseResources = parsed.VerboseResources,
                Extensions = parsed.Extensions
            });
        return 0;
    }
    catch (Exception exception)
    {
        return WriteError("Could not inspect portable device", exception);
    }
}

static PortableDeviceInfo? SelectDevice(
    IReadOnlyList<PortableDeviceInfo> devices,
    InspectionArguments arguments,
    out string? error)
{
    return SelectDeviceBySelection(devices, arguments.DeviceIndex, arguments.DeviceId, out error);
}

static PortableDeviceInfo? SelectDeviceBySelection(
    IReadOnlyList<PortableDeviceInfo> devices,
    int? deviceIndex,
    string? deviceId,
    out string? error)
{
    error = null;

    if (deviceId is not null)
    {
        PortableDeviceInfo? match = devices.FirstOrDefault(device =>
            device.Id.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            error = $"No portable device has ID '{deviceId}'.";
        }

        return match;
    }

    if (deviceIndex is not null)
    {
        if (deviceIndex < 1 || deviceIndex > devices.Count)
        {
            error = $"Device index must be between 1 and {devices.Count}.";
            return null;
        }

        return devices[deviceIndex.Value - 1];
    }

    if (devices.Count == 1)
    {
        return devices[0];
    }

    error = "Multiple portable devices were found. Select one with --device-index or --device-id.";
    return null;
}

static bool TryParseInspectionArguments(
    string[] arguments,
    out InspectionArguments? parsed,
    out string? error)
{
    int limit = 20;
    int depth = 4;
    int? deviceIndex = null;
    bool deviceIndexSpecified = false;
    string? deviceId = null;
    bool verboseResources = false;
    List<string> extensions = new();
    bool extensionSpecified = false;

    for (int index = 0; index < arguments.Length; index++)
    {
        string argument = arguments[index];
        if (argument.Equals("--verbose-resources", StringComparison.OrdinalIgnoreCase))
        {
            verboseResources = true;
            continue;
        }

        if (index + 1 >= arguments.Length)
        {
            parsed = null;
            error = $"Option '{argument}' requires a value.";
            return false;
        }

        string value = arguments[++index];
        if (argument.Equals("--limit", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(value, out limit) || limit < 1)
            {
                parsed = null;
                error = "--limit must be a positive integer.";
                return false;
            }
        }
        else if (argument.Equals("--depth", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(value, out depth) || depth < 0)
            {
                parsed = null;
                error = "--depth must be a non-negative integer.";
                return false;
            }
        }
        else if (argument.Equals("--device-index", StringComparison.OrdinalIgnoreCase))
        {
            if (deviceIndexSpecified)
            {
                parsed = null;
                error = "--device-index may only be specified once.";
                return false;
            }

            if (!int.TryParse(value, out int selectedIndex) || selectedIndex < 1)
            {
                parsed = null;
                error = "--device-index must be a positive integer.";
                return false;
            }

            deviceIndex = selectedIndex;
            deviceIndexSpecified = true;
        }
        else if (argument.Equals("--device-id", StringComparison.OrdinalIgnoreCase))
        {
            deviceId = value;
        }
        else if (argument.Equals("--extension", StringComparison.OrdinalIgnoreCase))
        {
            if (extensionSpecified)
            {
                parsed = null;
                error = "--extension may only be specified once.";
                return false;
            }

            if (!TryParseExtensionFilter(
                    value,
                    out MediaExtensionSelectionRule? inspectionExtensionFilter,
                    out error))
            {
                parsed = null;
                return false;
            }

            extensions.AddRange(inspectionExtensionFilter!.Extensions);
            extensionSpecified = true;
        }
        else
        {
            parsed = null;
            error = $"Unknown option '{argument}'.";
            return false;
        }
    }

    if (deviceIndex is not null && deviceId is not null)
    {
        parsed = null;
        error = "Use either --device-index or --device-id, not both.";
        return false;
    }

    parsed = new InspectionArguments(
        limit,
        depth,
        deviceIndex,
        deviceId,
        verboseResources,
        extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    error = null;
    return true;
}

static async ValueTask<IReadOnlyList<PortableDeviceInfo>> DiscoverDevicesAsync()
{
    IPortableDeviceDiscovery discovery = new WpdPortableDeviceDiscovery();
    return await discovery.GetDevicesAsync();
}

static void WriteDevices(IReadOnlyList<PortableDeviceInfo> devices)
{
    for (int index = 0; index < devices.Count; index++)
    {
        PortableDeviceInfo device = devices[index];
        Console.WriteLine($"[{index + 1}] {device.DisplayName} ({device.Id})");
        if (device.Manufacturer is not null)
        {
            Console.WriteLine($"    Manufacturer: {device.Manufacturer}");
        }

        if (device.Description is not null)
        {
            Console.WriteLine($"    Description: {device.Description}");
        }
    }
}

static int WriteError(string message, Exception exception)
{
    Console.Error.WriteLine($"{message}: {exception.Message}");
    return CliExitCodes.Failure;
}

static void WriteUsage()
{
    Console.Write(CliHelp.General);
}

static void WriteTimeTestUsage()
{
    Console.Write(CliHelp.TimeTest);
}

internal sealed record InspectionArguments(
    int Limit,
    int Depth,
    int? DeviceIndex,
    string? DeviceId,
    bool VerboseResources,
    IReadOnlyList<string> Extensions);

internal sealed record ListArguments(
    int? DeviceIndex,
    int Limit,
    bool ShowId,
    MediaExtensionSelectionRule? ExtensionFilter);

internal sealed record ImportOneArguments(
    int DeviceIndex,
    string MediaItemId,
    string TargetPath,
    ExistingFilePolicy Policy);

internal sealed record ImportArguments(
    int DeviceIndex,
    string TimeZone,
    string TargetTemplate,
    ExistingFilePolicy ExistingFilePolicy,
    LocalCaptureDateRange CaptureDateRange,
    MediaExtensionSelectionRule? ExtensionFilter);

internal sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}

internal static class CliExitCodes
{
    internal const int Success = 0;
    internal const int Failure = 1;
    internal const int UsageError = 2;
}
