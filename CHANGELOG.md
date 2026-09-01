# Changelog

All notable changes to MyMediaImport will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/).

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
[1.0.0]: https://github.com/Pizzy72/MyMediaImport/releases/tag/v1.0.0
