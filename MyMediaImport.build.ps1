param(
    [string] $BuildMode = "Release",
    [string] $Platform = "x64",
    [string[]] $CliArguments = @("help")
)

$SolutionPath = "MyMediaImport.sln"
$AppProjectPath = "src\MyMediaImport.App\MyMediaImport.App.csproj"
$CliProjectPath = "src\MyMediaImport.Cli\MyMediaImport.Cli.csproj"
$InstallerLicensePath = "artifacts\installer\DOTNET-LICENSE.txt"

# Synopsis: Clean and build MyMediaImport.
task Build {
    exec { dotnet clean $SolutionPath --configuration $BuildMode --property:Platform=$Platform }
    exec { dotnet build $SolutionPath --configuration $BuildMode --property:Platform=$Platform }
}

# Synopsis: Build and run the MyMediaImport desktop application.
task Run Build, {
    exec {
        dotnet run --project $AppProjectPath `
            --configuration $BuildMode `
            --no-build `
            --no-launch-profile
    }
}

# Synopsis: Build and run the MyMediaImport CLI.
task RunCli Build, {
    exec {
        dotnet run --project $CliProjectPath `
            --configuration $BuildMode `
            --no-build `
            --no-launch-profile `
            -- @CliArguments
    }
}

# Synopsis: Build MyMediaImport and run all tests.
task Test Build, {
    exec {
        dotnet test $SolutionPath `
            --configuration $BuildMode `
            --property:Platform=$Platform `
            --no-build `
            --logger "console;verbosity=detailed"
    }
}

# Synopsis: Build a self-contained MyMediaImport installer.
task Pack {
    $PublishPath = "artifacts\publish\app"
    $RuntimeLicensePath = Join-Path $PublishPath "DOTNET-LICENSE.txt"
    $WindowsDesktopLicensePath = Join-Path $PublishPath "DOTNET-WINDOWS-DESKTOP-LICENSE.txt"

    remove "artifacts\publish\app\*"
    remove "artifacts\publish\cli\*"
    remove "Setup\Output\*"

    exec { dotnet publish $AppProjectPath "-p:PublishProfile=win-x64" }
    exec { dotnet publish $CliProjectPath "-p:PublishProfile=win-x64" }

    New-Item -ItemType Directory -Force (Split-Path $InstallerLicensePath) | Out-Null

    $RuntimeLicenseText = [System.IO.File]::ReadAllText(
        $RuntimeLicensePath,
        [System.Text.UTF8Encoding]::new($false, $true))
    $WindowsDesktopLicenseText = [System.IO.File]::ReadAllText(
        $WindowsDesktopLicensePath,
        [System.Text.UTF8Encoding]::new($false, $true))
    $InstallerLicenseIntroduction = @"
MyMediaImport is open-source software licensed under the MIT License.

This installation includes Microsoft .NET runtime and Windows Desktop runtime
components. The license terms below apply to those Microsoft components and do
not replace the MyMediaImport MIT License.

-------------------------------------------------------------------------------
Microsoft.NETCore.App.Runtime.win-x64
-------------------------------------------------------------------------------

"@
    $WindowsDesktopHeading = @"

-------------------------------------------------------------------------------
Microsoft.WindowsDesktop.App.Runtime.win-x64
-------------------------------------------------------------------------------

"@

    [System.IO.File]::WriteAllText(
        $InstallerLicensePath,
        $InstallerLicenseIntroduction + $RuntimeLicenseText +
            $WindowsDesktopHeading + $WindowsDesktopLicenseText,
        [System.Text.UTF8Encoding]::new($true))

    exec { dotnet iscc "Setup\MyMediaImport.iss" }
}

# Synopsis: Build and launch the MyMediaImport installer.
task Install Pack, {
    $InstallerPath = Get-ChildItem "Setup\Output\MyMediaImport_*.exe" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $InstallerPath)
    {
        throw "Installer was not created."
    }

    exec { Start-Process -FilePath $InstallerPath.FullName -Wait }
}

# Synopsis: Test and package MyMediaImport.
task Release Test, Pack

task . Build
