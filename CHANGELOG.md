# Changelog

All notable changes to MyMediaImport will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/).

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

[1.0.0]: https://github.com/Pizzy72/MyMediaImport/releases/tag/v1.0.0
