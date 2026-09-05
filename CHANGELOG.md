# Changelog

All notable changes to MyMediaImport will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/).

## [1.3.0] - 2026-09-05

### Added

- A repository-local Invoke-Build environment for consistent build, test, run,
  packaging, and release workflows.
- An Inno Setup packaging task that installs the desktop application and CLI
  and displays the license terms for bundled Microsoft .NET components.
- An Invoke-Build task for building and launching the installer.
- A Start menu shortcut that opens a command prompt in the installed CLI
  directory and displays the CLI help.

## [1.2.0] - 2026-09-03

### Added

- A lazy-loading source folder picker with per-device remembered selections.
  Selected folders include their subfolders; missing or ambiguous saved paths
  stop preview loading rather than silently selecting the whole device.
- Optional `--source-folder` for CLI `list` and `import`, using the same folder
  resolution as the desktop application.
- Compact preview status showing the loaded source folder beside the media
  count, with the full path and preview details available in a tooltip.
- Filename tooltips show each media item's full device path, including nested
  folders, without increasing preview row heights.
- The `{captureOffset}` target path placeholder writes the resolved capture
  timezone offset without a colon, for example `+0200` or `-0530`, so it can be
  used safely in Windows filenames. It is available in the desktop template
  help and CLI import help.

### Changed

- Imported files now receive the resolved capture timestamp as their Windows
  file-system creation time, using the same capture timezone interpretation as
  target filenames. Embedded metadata such as EXIF and the last-write timestamp
  remain unchanged; skipped or already imported files are not modified.
- The target path preview now keeps showing the actual result paths after an
  import, including final collision-renamed paths, until the user changes the
  selection or loads a new preview. Starting a new selection immediately clears
  all paths from the previous import before calculating the new preview. A
  target file can be selected in File Explorer from the path's context menu.

### Fixed

- The target path context menu now follows the active light or dark theme, and
  selected path borders retain the same crisp thickness while the menu is open.
- The large media and target path preview borders no longer turn blue as focus
  moves between the lists; focus and selection remain visible on individual
  rows and controls.

## [1.1.0] - 2026-09-01

### Added

- Live byte-level progress reporting while media files are transferred.
- Distinct themed status presentations for successful, cancelled, and failed
  imports in both light and dark mode.

### Changed

- Import progress now shows the transferred size relative to the total size of
  the complete import instead of only updating after each file.
- Transfer sizes use a stable unit, fixed-width formatting, and preallocated
  layout space to prevent text and controls from shifting during an import.
- Progress updates are throttled to keep the interface responsive while still
  reporting the exact final byte count.
- Cancelled imports now clearly distinguish completed files from discarded
  partial transfers and retain an accurate partial progress indicator.

### Fixed

- Prevented delayed progress notifications from making transferred byte counts
  appear to move backwards.
- Kept disabled media lists consistently theme-colored during imports instead
  of displaying a light system background in dark mode.

## [1.0.0] - 2026-08-28

### Added

- Native Windows desktop application and command-line interface.
- Discovery and access for media sources exposed through Windows Portable
  Devices (WPD).
- Lazy photo previews with fallback generation from original images when a
  device does not provide thumbnails.
- Media selection by capture date, file extension, and media type.
- Explicit handling of local, UTC, fixed-offset, and unknown-offset capture
  times.
- Configurable target path templates with preview support.
- Skip, rename, and overwrite policies for existing files.
- Safe imports through temporary partial files and transferred-size
  validation.
- Light, dark, and system themes with configurable interface text size.
- Self-contained publishing for 64-bit Windows.

[1.1.0]: https://github.com/Pizzy72/MyMediaImport/compare/v1.0.0...v1.1.0
[1.2.0]: https://github.com/Pizzy72/MyMediaImport/compare/v1.1.0...v1.2.0
[1.3.0]: https://github.com/Pizzy72/MyMediaImport/compare/v1.2.0...v1.3.0
[Unreleased]: https://github.com/Pizzy72/MyMediaImport/compare/v1.3.0...HEAD
[1.0.0]: https://github.com/Pizzy72/MyMediaImport/releases/tag/v1.0.0
