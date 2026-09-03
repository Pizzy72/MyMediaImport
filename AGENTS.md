# MyMediaImport

## Project purpose

MyMediaImport is a small and fast Windows application for importing
photos and videos from portable devices and other media sources.

It is an importer, not a photo management application.

The primary use case is:

1. Connect a media source.
2. Quickly display the newest photos and videos.
3. Select media to import.
4. Preview target paths and renamed files.
5. Import them into a configurable directory structure.

Example target:

    E:\Bilder\2026\08\2026-08-22_153023Z.heic

## Projects

The solution consists of the following projects:

    src/MyMediaImport.Core
    src/MyMediaImport.Windows
    src/MyMediaImport.App
    src/MyMediaImport.Cli
    tests/MyMediaImport.Core.Tests

### MyMediaImport.Core

Class library containing all platform-independent domain and import logic.

Core must not reference WPF, WPD, COM, or other Windows-specific APIs.

Core contains concepts such as:

- media items and media sources
- media selection rules
- import requests, plans, and results
- path template expansion
- local and UTC time handling
- collision handling
- existing-file policies
- import validation

Core should contain deterministic logic wherever practical so that it
can be tested without Windows devices, WPF, or file-system side effects.

### MyMediaImport.Windows

Class library containing Windows-specific implementations.

This includes:

- Windows Portable Devices (WPD)
- COM interop
- portable-device discovery
- Windows-specific media sources
- Windows-specific file-system services where required

MyMediaImport.Windows references:

    MyMediaImport.Core

Do not expose WPD-, COM-, iPhone-, iOS-, or other Windows-specific
concepts through the public Core API.

### MyMediaImport.App

WPF desktop application.

The application project contains presentation and user-interface logic.

It references:

    MyMediaImport.Core
    MyMediaImport.Windows

Do not duplicate domain or import logic in the WPF application.

The WPF application is one presentation layer over the shared import
functionality.

### MyMediaImport.Cli

Console application providing command-line access to the same import
functionality as the WPF application.

It references:

    MyMediaImport.Core
    MyMediaImport.Windows

Do not duplicate domain or import logic in the CLI.

Core and Windows services must not require interactive WPF user-interface
elements such as windows, dialogs, or MessageBox calls.

The architecture must allow imports to run entirely from the command
line, scripts, shortcuts, or Windows Task Scheduler.

### MyMediaImport.Core.Tests

Automated test project located at:

    tests/MyMediaImport.Core.Tests

It references:

    MyMediaImport.Core

Use this project primarily for deterministic Core logic such as:

- path template expansion
- local time and UTC handling
- collision naming
- existing-file policies
- media selection rules
- import planning
- import validation

The test project must not require a connected physical device.

Do not add third-party test frameworks without explicit approval.
Prefer Microsoft-supported test infrastructure if a test framework is
needed.

The test project must be part of the solution.

### Dependency direction

Keep project dependencies simple and one-directional:

    MyMediaImport.Core
        ↑
        |
    MyMediaImport.Windows
        ↑
        |
        +--------------------+
        |                    |
    MyMediaImport.App    MyMediaImport.Cli

    MyMediaImport.Core
        ↑
        |
    MyMediaImport.Core.Tests

MyMediaImport.Core must remain independent of all other projects.

MyMediaImport.Windows may depend on Core, but Core must never depend on
Windows.

App and Cli may depend on Core and Windows, but Core and Windows must
never depend on App or Cli.

Tests may depend on Core, but production projects must never depend on
test projects.

## Dependencies

Avoid third-party NuGet packages.

Do not add a third-party dependency without explicit approval.

Prefer:

- .NET standard libraries
- Windows APIs
- small project-owned adapters around native Windows APIs

In particular, do not introduce third-party libraries for WPD,
command-line parsing, MVVM, image processing, logging, dependency
injection, or databases unless explicitly requested.

## Media sources

Media sources must be abstracted behind interfaces in Core.

Do not expose iPhone-, iOS-, WPD-, or Windows-specific concepts through
the Core API.

The architecture should allow additional sources later, such as:

- iPhone
- Android devices
- digital cameras
- SD cards
- local directories

The initial portable-device implementation uses Windows Portable
Devices (WPD).

## Asynchronous I/O

Use asynchronous APIs for I/O where appropriate.

I/O operations should accept CancellationToken where practical.

Do not block the WPF UI thread with device access, file access, image
decoding, or other potentially slow operations.

## Preview performance

Do not transfer complete original media files merely to display gallery
thumbnails when a smaller device resource is available.

Load thumbnails lazily.

Only load thumbnails that are visible or likely to become visible soon.

Limit concurrent device requests.

Use UI virtualization.

Do not keep an unbounded number of decoded images or media objects in
memory.

Memory consumption should remain reasonably bounded regardless of the
total number of media files on a device.

## Path templates

Target paths and filenames are generated from templates.

The design should support expressions such as:

    E:\Bilder\{captureUtc:yyyy}\{captureUtc:MM}\{captureUtc:yyyy-MM-dd_HHmmss'Z'}{collision:_00}.{ext}

Initial placeholders include:

    {capture:...}
    {captureUtc:...}
    {original}
    {ext}
    {collision:...}

capture represents local capture time.

captureUtc represents UTC capture time.

A literal Z in a generated filename must only be used for an actual UTC
timestamp.

Path preview and actual import must use exactly the same path-generation
implementation.

## Collision handling

Existing-file behavior is represented explicitly, for example:

    Skip
    Rename
    Overwrite

A collision placeholder such as:

    {collision:_00}

produces no suffix for the first file and suffixes such as:

    _02
    _03

when renaming is required.

Different file extensions do not constitute a collision.

When Overwrite is selected, an existing target must be replaced rather
than generating _02, _03, etc.

Never silently overwrite files unless the caller explicitly selected
Overwrite behavior.

## Safe imports

Never transfer directly into the final target filename.

Transfer into a temporary .partial file first.

Count the bytes transferred and compare the result with the expected
file size reported by the source when that size is available.

Only publish the final target file after a successful transfer.

Do not implement hashing unless explicitly requested.

When overwriting an existing file, do not destroy the existing valid
file before the replacement has been transferred successfully.

## Date and time handling

MyMediaImport must support both local time and UTC consistently.

Internally, prefer DateTimeOffset for timestamps where the UTC offset
is known.

Do not silently discard timezone or UTC offset information.

The application must distinguish between:

- local capture time
- UTC capture time

Path templates support both:

    {capture:yyyy-MM-dd_HHmmss}
    {captureUtc:yyyy-MM-dd_HHmmss'Z'}

A literal Z suffix must only be generated for a timestamp that has
actually been converted to UTC.

Example:

    Local: 2026-08-22 17:30:23 +02:00
    UTC:   2026-08-22 15:30:23Z

The user interface should allow timestamps to be displayed in either
local time or UTC where appropriate.

CLI date/time filters must have clearly defined timezone semantics.
Options such as --today should refer to the user's local calendar day
unless explicitly documented otherwise.

Do not assume that device timestamps always contain reliable timezone
information. Preserve the original timestamp information where
possible and handle missing timezone information explicitly rather
than silently treating it as UTC.

## Capture timezone handling

Some media sources, including WPD/iPhone, may provide local capture
wall-clock times without timezone or UTC offset information.

MyMediaImport must never silently assume that such timestamps are UTC.

The import configuration must allow the user to specify how timestamps
with an unknown offset are interpreted.

Initial supported timezone specifications should include:

    local
    +02:00
    -05:00

"local" means the current Windows local timezone. The offset must be
resolved for the actual capture date, including daylight-saving rules.

Resolving a local wall-clock time may be ambiguous during the transition
from daylight-saving time to standard time, or invalid during the
transition from standard time to daylight-saving time.

Do not silently choose an offset for an ambiguous local time and do not
silently adjust an invalid local time. Return a clear validation or
planning error so that the caller can request an explicit interpretation.

A fixed UTC offset applies exactly as specified and does not include
daylight-saving rules.

The resolved capture timestamp should preserve both:

- local capture time
- resolved UTC offset

and must allow conversion to UTC.

The domain model must distinguish between:

- a local wall-clock capture time whose offset is unknown
- a capture timestamp whose offset is known
- a timestamp resolved from an unknown-offset value using the selected
timezone interpretation

Do not force a timestamp with an unknown offset into DateTimeOffset by
inventing UTC or the current system offset. Preserve the original
wall-clock value until the timezone interpretation has been applied.

Path templates using {captureUtc:...} are only valid when the capture
timezone has been resolved.

Timezone selection must be available to both CLI and WPF.

The design should allow timezone settings to become part of reusable
import profiles later.

### File extension normalization

Target file extensions must be normalized to lowercase.

The `{ext}` placeholder represents the source file extension:

- without the leading dot
- normalized to lowercase using culture-independent rules

Examples:

    IMG_1234.JPG   -> {ext} = jpg
    IMG_1235.HEIC  -> {ext} = heic
    IMG_1236.Mov   -> {ext} = mov

Therefore:

    {captureUtc:yyyy-MM-dd_HHmmss'Z'}.{ext}

produces, for example:

    2026-08-23_125430Z.jpg

The original spelling of the source file extension must not affect the
generated target filename.

Do not rename one media format to another. Extension normalization only
changes the casing of the existing extension.

If access to the original extension spelling is needed later, introduce
a separate placeholder such as `{originalExt}` rather than changing the
semantics of `{ext}`.

## Database

Do not introduce a database at this stage.

MyMediaImport is not a media catalog.

Use simple persistent settings where necessary, preferably using
standard .NET functionality such as System.Text.Json.

## CLI

Keep all import operations usable from a command-line application.

Expected future commands include forms such as:

    MyMediaImport.Cli.exe devices
    MyMediaImport.Cli.exe list
    MyMediaImport.Cli.exe import --today
    MyMediaImport.Cli.exe import --today --overwrite

Do not require user-interface interaction from Core or Windows services.

Do not show MessageBox dialogs outside the WPF presentation layer.

Return errors and results to the caller so that both WPF and CLI can
handle them appropriately.

CLI commands should eventually provide meaningful process exit codes.

## Scope

Keep the application focused.

Do not add photo-management features such as:

- ratings
- albums
- tags
- image editing
- face recognition
- media cataloging

unless explicitly requested.

Prefer simple implementations over speculative infrastructure.

Do not add abstractions merely because they might be useful someday.
Add abstractions where they protect an already identified boundary,
especially media sources, platform-specific services, and presentation
layers.

## User interface themes

The WPF application must support both light and dark themes from the
beginning.

Do not hard-code presentation colors directly in controls, windows,
styles, or templates.

Define colors and brushes as centralized theme resources so that the
active theme can be changed without rebuilding the user interface.

The initial design should support:

- Light
- Dark
- System

System should follow the current Windows application theme where
practical.

All custom controls, icons, selection states, hover states, disabled
states, warnings, errors, and progress indicators must remain readable
in both light and dark themes.

Prefer simple WPF resource dictionaries and standard .NET/Windows
functionality. Do not introduce a third-party theming library unless
explicitly requested.

Theme handling belongs to the presentation layer and must not introduce
WPF dependencies into MyMediaImport.Core.

## User interface scaling and font size

The WPF application must support a user-configurable base font size.

Use 12 pt as the initial default font size.

Do not hard-code font sizes throughout individual controls, styles,
or templates.

Define the base font size as a centralized application resource so
that changing it consistently affects the user interface.

The user must be able to change the base font size in the application
settings without restarting the application.

Persist the selected font size as a user setting.

Controls, dialogs, lists, tables, buttons, menus, and other UI elements
must remain usable when the font size is increased.

Avoid layouts with fixed heights or widths that would clip text when
the font size changes.

Prefer layouts that adapt naturally to their content.

Icons and other relevant UI elements should remain visually balanced
when larger font sizes are selected.

Respect Windows DPI scaling and do not implement custom DPI scaling
unless necessary.

## CLI online help

The CLI help must be primarily user-oriented and easy to read.

Formal EBNF syntax is still required, but it must not dominate the
default help output.

Provide three help levels:

    MyMediaImport.Cli.exe help
    MyMediaImport.Cli.exe help <command>
    MyMediaImport.Cli.exe help <command> syntax

### General help

The general help should contain:

- usage
- available commands
- short descriptions
- important common options
- a few realistic examples

Do not show full EBNF grammar in the general help.

### Command help

Command-specific help should contain:

- usage
- required options
- optional options
- relevant validation rules
- defaults
- concise examples

Prefer clear user-oriented wording over formal grammar.

For example:

    Time selection (choose one):

is preferred over exposing grammar productions directly.

At the end of command help, include a short reference to the formal
syntax, for example:

    For the formal syntax:
      MyMediaImport.Cli.exe help import syntax

### Formal syntax

The command:

    MyMediaImport.Cli.exe help <command> syntax

must display the formal EBNF grammar for that command.

The EBNF grammar must accurately describe the syntax accepted by the
actual command-line parser.

Validation rules that cannot be expressed cleanly in readable EBNF may
be documented separately.

Prefer readable EBNF over excessively complicated productions.

### Presentation

Use ASCII characters only throughout all help output.

Use:

- consistent indentation
- blank lines between logical sections
- aligned option descriptions where useful
- reasonably short line lengths
- realistic executable examples

Do not use:

- Unicode symbols
- typographic quotes
- box-drawing characters
- decorative ASCII boxes
- ANSI colors as a requirement for readability

The help must remain readable when redirected to a plain text file.

Whenever CLI syntax changes, update:

- the parser
- the user-oriented help
- the formal EBNF
- examples
- related tests

in the same change.

## Development

Keep public APIs small.

Prefer immutable domain objects where practical.

Build the complete solution after meaningful changes.

Do not suppress compiler warnings merely to make the build clean.
Investigate their cause.

Before making a significant architectural change, explain the reason
and tradeoffs rather than silently changing the established design.

## Tests

Create and maintain a dedicated test project:

    tests/MyMediaImport.Core.Tests

The test project is part of the solution and references
MyMediaImport.Core.

Use it for deterministic Core logic such as:

- path template expansion
- local time and UTC handling
- collision naming
- existing-file policies
- media selection rules
- import planning and validation

Do not add third-party test frameworks without explicit approval.
Prefer Microsoft-supported test infrastructure if a test framework is
needed.

The complete solution build must include the test project.

Run the test suite after meaningful Core changes.

## Development environment

The primary development environment is Visual Studio 2026 on Windows.

Use SDK-style .NET projects and solution/project files that work
normally with Visual Studio 2026.

Use the current stable .NET SDK installed on the development machine.
Do not require Visual Studio preview features unless explicitly requested.

Keep the solution buildable both from Visual Studio and with:

    dotnet build

## C# coding conventions

Use the following naming and member-access conventions consistently.

### Private fields

Private instance fields must begin with an underscore.

Examples:

    private readonly IMediaSource _mediaSource;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _selectedCount;

Do not use prefixes such as:

    m_
    s_

unless a different convention is explicitly required for a special case.

### Member access

Do not qualify instance members with `this.` unless it is required to
resolve an ambiguity.

Prefer:

    _mediaSource
    SelectedItems
    Refresh()

instead of:

    this._mediaSource
    this.SelectedItems
    this.Refresh()

If a constructor or method parameter has the same logical name as a
field, keep the parameter unprefixed and assign it to the underscored
field.

Example:

    public ImportService(IMediaSource mediaSource)
    {
        _mediaSource = mediaSource;
    }

Do not rename parameters merely to avoid this pattern.

### Other Rules

Enable nullable reference types.

Use implicit usings where appropriate.

Avoid `var`. Always declare local variables with their explicit type.

When creating objects and the type is clear from the target context,
use the target-typed `new()` operator instead of repeating the type.


## Git workflow

After completing a coherent development step:

- run the relevant build and tests
- review git status
- leave the changes uncommitted so the user can review them manually
- ask the user to review the completed changes before every commit
- do not stage or commit changes until the user has completed the manual
  review and explicitly requested the commit
- after that explicit request, stage only changes related to the current task
- create one focused commit
- use a short, descriptive commit message in imperative form
- do not combine unrelated changes in one commit
- do not rewrite or amend existing commits unless explicitly requested
