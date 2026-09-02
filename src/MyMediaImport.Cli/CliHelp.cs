namespace MyMediaImport.Cli;

public static class CliHelp
{
    private static string GeneralDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe <command> [options]

        Commands:
          devices       List portable devices
          list          List media files on a device
          import        Import multiple media files
          import-one    Import a single media file
          inspect       Inspect WPD objects (diagnostic/development)
          time-test     Test capture-time resolution (diagnostic/development)
          help          Show general or command-specific help

        Syntax (EBNF):
          invocation   = program, command ;
          program      = "MyMediaImport.Cli.exe" ;
          command      = "devices" | "list" | "import" | "import-one"
                       | "inspect" | "time-test" | "help" ;

        Options:
          Command and option names are matched ignoring ASCII letter case.
          Use "MyMediaImport.Cli.exe help <command>" for requirements,
          validation rules, defaults, and command-specific examples.

          --device-index N     Select a portable device by one-based index.
          --timezone SPEC      Use local or a fixed +HH:mm/-HH:mm offset.
                               Fixed offsets have no daylight-saving rules.
          --extension LIST     Include comma-separated extensions without dots.
          --source-folder PATH Limit list/import to a folder and its subfolders.
          --target TEMPLATE    Generate complete destination paths.
          --skip-existing      Skip existing targets (import default).
          --rename-existing    Rename genuine collisions; skip identical files.
          --overwrite          Replace existing targets safely.

        Examples:
          MyMediaImport.Cli.exe devices

          MyMediaImport.Cli.exe list --device-index 1 --extension JPG,HEIC

          MyMediaImport.Cli.exe import --device-index 1 --today ^
              --extension JPG,HEIC ^
              --timezone local ^
              --target "E:\Bilder\{capture:yyyy}\{capture:MM}\{original}.{ext}"

        """);

    public static string General => CreatePracticalHelp(GeneralDocument, null);

    public static string Devices => CreatePracticalHelp(DevicesDocument, "devices");

    public static string List => CreatePracticalHelp(ListDocument, "list");

    public static string Import => CreatePracticalHelp(ImportDocument, "import");

    public static string ImportOne => CreatePracticalHelp(ImportOneDocument, "import-one");

    public static string Inspect => CreatePracticalHelp(InspectDocument, "inspect");

    public static string TimeTest => CreatePracticalHelp(TimeTestDocument, "time-test");

    public static string Help => CreatePracticalHelp(HelpDocument, "help");

    public static string ImportSyntax => CreateSyntaxHelp(ImportDocument, "import");

    private static string DevicesDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe devices

        Syntax (EBNF):
          devices-command = "devices" ;

        Options:
          None.

        Examples:
          MyMediaImport.Cli.exe devices

        """);

    private static string ListDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe list [options]

        Syntax (EBNF):
          list-command   = "list", { list-basic-option },
                           [ extension-option ], { list-basic-option } ;
          list-basic-option = device-option | limit-option | "--show-id"
                            | "--source-folder", folder-path ;
          folder-path = ? one argument of non-empty folder names separated by
                          / or backslash, with no '.' or '..' segments ? ;
          device-option  = "--device-index", positive-integer ;
          limit-option   = "--limit", positive-integer ;
          extension-option = "--extension", extension-list ;
          extension-list = extension, { ",", extension } ;
          extension      = alphanumeric, { alphanumeric } ;
          positive-integer = non-zero-digit, { digit } ;
          alphanumeric   = letter | digit ;
          letter         = ? ASCII letter A-Z or a-z ? ;
          non-zero-digit = "1" | "2" | "3" | "4" | "5"
                         | "6" | "7" | "8" | "9" ;
          digit          = "0" | non-zero-digit ;

        Options:
          --device-index N     Select by one-based index. Required when more
                               than one portable device is connected.
          --limit N            Maximum displayed items. Default: 50.
          --extension LIST     Include only listed extensions, ignoring case.
                               Do not include a leading dot.
          --show-id            Include each WPD media object ID.
          --source-folder PATH Device-relative path, including the storage name.
                               Includes subfolders. Default: all folders.

        Validation:
          Options may appear in any order. --extension may occur at most once.
          --device-index may occur at most once.
          --source-folder may occur at most once. Names match exactly.
          Use / or backslash between names; no leading/trailing separator,
          empty segment, '.' or '..'. Quote paths containing spaces.
          A missing or ambiguous folder is an error, not a fallback to all.
          If multiple devices are connected, --device-index is required.
          Repeated --limit options use the last value; repeated --show-id
          options have the same effect as one occurrence.

        Examples:
          MyMediaImport.Cli.exe list
          MyMediaImport.Cli.exe list --device-index 1 --limit 20
          MyMediaImport.Cli.exe list --device-index 1 ^
              --source-folder "Internal Storage/DCIM/Camera"
          MyMediaImport.Cli.exe list --device-index 1 --extension JPG,HEIC

        """);

    private static string ImportDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe import [options]

        Syntax (EBNF):
          import-command = "import", { import-option } ;
          import-option  = "--device-index", positive-integer
                         | short-time-range
                         | date-range-option
                         | "--timezone", timezone
                         | "--target", path-template
                         | "--extension", extension-list
                         | "--source-folder", folder-path
                         | existing-file-option ;
          folder-path = ? one argument of non-empty folder names separated by
                          / or backslash, with no '.' or '..' segments ? ;
          short-time-range = "--today"
                         | "--yesterday"
                         | "--last", duration
                         | "--all" ;
          date-range-option = "--from", date | "--to", date ;
          duration       = positive-integer, ( "d" | "D" ) ;
          date           = four-digits, "-", two-digits, "-", two-digits ;
          timezone       = "local" | signed-offset ;
          signed-offset  = ( "+" | "-" ), two-digits, ":", two-digits ;
          extension-list = extension, { ",", extension } ;
          extension      = alphanumeric, { alphanumeric } ;
          existing-file-option = "--skip-existing"
                         | "--rename-existing"
                         | "--overwrite" ;
          positive-integer = non-zero-digit, { digit } ;
          four-digits    = digit, digit, digit, digit ;
          two-digits     = digit, digit ;
          alphanumeric   = letter | digit ;
          letter         = ? ASCII letter A-Z or a-z ? ;
          non-zero-digit = "1" | "2" | "3" | "4" | "5"
                         | "6" | "7" | "8" | "9" ;
          digit          = "0" | non-zero-digit ;
          path-template  = ? one non-empty command-line argument ? ;

        Options:
          Required:
            --device-index N   Select a portable device by one-based index.
            time selection     Required; choose one of the choices below.
            --timezone SPEC    Use local or a fixed +HH:mm/-HH:mm offset.
                               Fixed offsets have no daylight-saving rules.
            --target TEMPLATE  Generate the complete destination path.

        Time selection (choose one):
          --today              Current local calendar day.
          --yesterday          Previous local calendar day.
          --last Nd            N local calendar days including today.
          --from yyyy-MM-dd    Inclusive lower bound; may be used with --to.
          --to yyyy-MM-dd      Inclusive upper bound; may be used with --from.
          --all                No capture-date restriction.

          Optional:
            --source-folder PATH Device-relative path including the storage name.
                                 Includes subfolders. Default: all folders.
            --extension LIST   Include only listed extensions, ignoring case.
                               Do not include leading dots.
            --skip-existing    Skip existing targets. This is the default.
            --rename-existing  Rename genuine collisions and detect identical
                               files to keep repeated imports idempotent.
            --overwrite        Replace existing targets safely.

        Validation:
          Options may appear in any order.
          Exactly one --device-index, exactly one time selection,
          exactly one --timezone, and exactly one --target are required.
          A bounded time selection consists of --from, --to, or both.
          The short forms cannot be combined with each other or with
          --from/--to. Duplicate --from or --to options are invalid, and
          --from must not be after --to. Dates use yyyy-MM-dd.
          Specify at most one --extension and at most one existing-file option.
          --source-folder may occur at most once. Names match exactly.
          Use / or backslash between names; no leading/trailing separator,
          empty segment, '.' or '..'. Quote paths containing spaces.
          A missing or ambiguous folder is an error, not a fallback to all.

        TARGET template:
          {capture:FORMAT}      Local capture time.
          {captureUtc:FORMAT}   Capture time converted to UTC.
          {original}            Original filename without extension.
          {ext}                 Normalized lowercase extension without dot.
          {collision:FORMAT}    Empty, then suffixes such as _02 and _03.

          FORMAT uses standard .NET custom date and time formats.

        Examples:
          MyMediaImport.Cli.exe import --device-index 1 --today ^
              --timezone local ^
              --target "E:\Bilder\{capture:yyyy}\{capture:MM}\{original}.{ext}"

          MyMediaImport.Cli.exe import --device-index 1 --last 7d ^
              --source-folder "Internal Storage/DCIM/Camera" ^
              --extension JPG,HEIC,MOV ^
              --timezone +02:00 ^
              --rename-existing ^
              --target "E:\Bilder\{captureUtc:yyyy}\{captureUtc:yyyyMMdd_HHmmss'Z'}{collision:_00}.{ext}"

        """);

    private static string ImportOneDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe import-one [options]

        Syntax (EBNF):
          import-one-command = "import-one", { import-one-option } ;
          import-one-option  = "--device-index", positive-integer
                             | "--id", media-id
                             | "--target", path
                             | "--policy", existing-file-policy ;
          existing-file-policy = "skip" | "rename" | "overwrite" ;
          positive-integer   = non-zero-digit, { digit } ;
          non-zero-digit     = "1" | "2" | "3" | "4" | "5"
                             | "6" | "7" | "8" | "9" ;
          digit              = "0" | non-zero-digit ;
          media-id           = ? one non-empty command-line argument ? ;
          path               = ? one non-empty command-line argument ? ;

        Options:
          Required:
            --device-index N   Select a portable device by one-based index.
            --id ID            Select one WPD media object ID.
            --target PATH      Set the complete literal destination path.

          Optional:
            --policy POLICY    skip, rename, or overwrite. Default: skip.

        Validation:
          Options may appear in any order. Device index must be positive;
          ID and target must not be empty. --device-index and --target may
          occur at most once. Repeated --id and --policy options use the last
          value.

        Examples:
          MyMediaImport.Cli.exe import-one --device-index 1 --id o1C4 ^
              --target "C:\Temp\IMG_9220.jpg" --policy rename

        """);

    private static string InspectDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe inspect [options]

        Syntax (EBNF):
          inspect-command = "inspect", { inspect-option } ;
          inspect-option  = "--device-index", positive-integer
                          | "--device-id", device-id
                          | "--limit", positive-integer
                          | "--depth", non-negative-integer
                          | "--extension", extension-list
                          | "--verbose-resources" ;
          extension-list = extension, { ",", extension } ;
          extension      = alphanumeric, { alphanumeric } ;
          alphanumeric   = letter | digit ;
          letter         = ? ASCII letter A-Z or a-z ? ;
          positive-integer = non-zero-digit, { digit } ;
          non-negative-integer = digit, { digit } ;
          non-zero-digit   = "1" | "2" | "3" | "4" | "5"
                           | "6" | "7" | "8" | "9" ;
          digit            = "0" | non-zero-digit ;
          device-id        = ? one command-line argument ? ;

        Options:
          --device-index N       Select by one-based index.
          --device-id ID         Select by WPD device ID.
          --limit N              Maximum relevant objects. Default: 20.
          --depth N              Maximum object-tree depth. Default: 4.
          --extension LIST       Filter diagnostic file extensions.
          --verbose-resources    Show additional resource properties.

        Validation:
          Use either --device-index or --device-id, not both. If neither is
          given, exactly one connected device is required. At most one
          --extension option is accepted; it requires one or more extensions
          without leading dots.
          --device-index may occur at most once. Repeated --device-id,
          --limit, and --depth options use the last value. Repeating
          --verbose-resources has the same effect as one occurrence.

        Examples:
          MyMediaImport.Cli.exe inspect --device-index 1 --limit 20 --depth 4
          MyMediaImport.Cli.exe inspect --device-index 1 --extension HEIC,MOV ^
              --verbose-resources

        """);

    private static string TimeTestDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe time-test [options]

        Syntax (EBNF):
          time-test-command = "time-test", capture-option, timezone-option ;
          capture-option    = "--capture", local-timestamp ;
          timezone-option   = "--timezone", timezone ;
          timezone          = "local" | signed-offset ;
          signed-offset     = ( "+" | "-" ), two-digits, ":", two-digits ;
          local-timestamp   = date, "T", time, [ fraction ] ;
          fraction          = ".", digit,
                            [ digit, [ digit, [ digit, [ digit,
                            [ digit, [ digit ] ] ] ] ] ] ;
          date              = four-digits, "-", two-digits, "-", two-digits ;
          time              = two-digits, ":", two-digits, ":", two-digits ;
          four-digits       = digit, digit, digit, digit ;
          two-digits        = digit, digit ;
          digit             = "0" | "1" | "2" | "3" | "4"
                            | "5" | "6" | "7" | "8" | "9" ;

        Options:
          Required:
            --capture TIMESTAMP  Local timestamp, e.g. 2026-08-23T14:53:20.
            --timezone SPEC      local or a fixed offset from -14:00 to +14:00.

          Optional:
            None.

          TIMESTAMP accepts zero through seven fractional second digits.

        Validation:
          Options may appear in either order. Each required option must occur
          exactly once. Fixed offsets range from -14:00 through +14:00.
          Fixed offsets have no daylight-saving rules.

        Examples:
          MyMediaImport.Cli.exe time-test --capture 2026-08-23T14:53:20 ^
              --timezone +02:00

        """);

    private static string HelpDocument => NormalizeNewlines(
        """
        Usage:
          MyMediaImport.Cli.exe help [command [syntax]]

        Syntax (EBNF):
          help-command = "help", [ help-topic, [ "syntax" ] ] ;
          help-topic   = "devices" | "list" | "import" | "import-one"
                       | "inspect" | "time-test" | "help" ;

        Options:
          command       Show practical command-specific help.
          syntax        Show the command's formal EBNF syntax.

        Validation:
          The syntax keyword requires a preceding help topic. The topic must
          name a listed command.

        Examples:
          MyMediaImport.Cli.exe help
          MyMediaImport.Cli.exe help import
          MyMediaImport.Cli.exe help import syntax
          MyMediaImport.Cli.exe help list

        """);

    public static bool TryGetCommandHelp(string command, out string help)
    {
        help = command.ToLowerInvariant() switch
        {
            "devices" => Devices,
            "list" => List,
            "import" => Import,
            "import-one" => ImportOne,
            "inspect" => Inspect,
            "time-test" => TimeTest,
            "help" => Help,
            _ => string.Empty
        };
        return help.Length > 0;
    }

    public static bool TryGetCommandSyntax(string command, out string help)
    {
        string? document = GetCommandDocument(command);
        help = document is null ? string.Empty : CreateSyntaxHelp(document, command.ToLowerInvariant());
        return help.Length > 0;
    }

    private static string? GetCommandDocument(string command) =>
        command.ToLowerInvariant() switch
        {
            "devices" => DevicesDocument,
            "list" => ListDocument,
            "import" => ImportDocument,
            "import-one" => ImportOneDocument,
            "inspect" => InspectDocument,
            "time-test" => TimeTestDocument,
            "help" => HelpDocument,
            _ => null
        };

    private static string CreatePracticalHelp(string document, string? command)
    {
        int syntaxStart = document.IndexOf("Syntax (EBNF):", StringComparison.Ordinal);
        int optionsStart = document.IndexOf("Options:", syntaxStart, StringComparison.Ordinal);
        string practical = document.Remove(syntaxStart, optionsStart - syntaxStart).TrimEnd();
        if (command is null)
        {
            return practical + Environment.NewLine;
        }

        return practical + Environment.NewLine + Environment.NewLine +
               "For the formal syntax:" + Environment.NewLine +
               $"  MyMediaImport.Cli.exe help {command} syntax" + Environment.NewLine;
    }

    private static string CreateSyntaxHelp(string document, string command)
    {
        int syntaxStart = document.IndexOf("Syntax (EBNF):", StringComparison.Ordinal);
        int optionsStart = document.IndexOf("Options:", syntaxStart, StringComparison.Ordinal);
        string syntax = document[syntaxStart..optionsStart].TrimEnd();
        return "Usage:" + Environment.NewLine +
               $"  MyMediaImport.Cli.exe help {command} syntax" +
               Environment.NewLine + Environment.NewLine +
               syntax + Environment.NewLine;
    }

    private static string NormalizeNewlines(string value) =>
        value.ReplaceLineEndings(Environment.NewLine);
}
