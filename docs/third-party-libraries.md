# Third-Party Libraries

This document records the NuGet and runtime dependencies used by
MyMediaImport. The package inventory was verified with:

```powershell
dotnet list MyMediaImport.sln package --include-transitive
```

## Application and CLI

The four production projects have no direct or transitive NuGet package
references. Application functionality is implemented with project-owned code,
.NET, WPF, Windows Forms, and Windows APIs.

The self-contained Windows x64 publish outputs bundle these Microsoft runtime
packages at the version pinned by the publish profiles:

- `Microsoft.NETCore.App.Runtime.win-x64` 10.0.11
- `Microsoft.WindowsDesktop.App.Runtime.win-x64` 10.0.11 (desktop application)

Their package metadata declares the MIT License. Installer staging copies the
version-specific license files and the .NET runtime third-party notices into
the distribution. The Inno Setup installer shows the bundled runtime license
terms before installation and installs those files with the application.

## Test Infrastructure

Each test project references `MSTest` 4.0.2. Its transitive packages provide
the Microsoft test platform, test host, MSTest framework and adapter, code
coverage, telemetry and TRX reporting extensions, plus their supporting
dependencies. These packages are build- and test-only and are not included in
the application, CLI publish output, or installer.

The transitive test graph includes `Newtonsoft.Json` 13.0.3, but it is used only
by the test infrastructure and is not a production dependency.

## Build Tools

Invoke-Build 5.14.22 and the `dotnet-innosetup` 6.2.1 tool are repository-local
build tools. They create artifacts but are not distributed in the installer.
