[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
[xml] $buildProperties = Get-Content -LiteralPath (Join-Path $repositoryRoot "Directory.Build.props") -Raw
[string] $version = $buildProperties.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Directory.Build.props must define a Version for the publish archive."
}

$publishRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot "artifacts\publish"))
$publishDirectories = @(
    (Join-Path $publishRoot "app"),
    (Join-Path $publishRoot "cli")
)

foreach ($publishDirectory in $publishDirectories) {
    $resolvedPublishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)
    $expectedPrefix = $publishRoot.TrimEnd("\") + "\"

    if (-not $resolvedPublishDirectory.StartsWith(
            $expectedPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean publish directory outside '$publishRoot'."
    }

    if (Test-Path -LiteralPath $resolvedPublishDirectory) {
        Remove-Item -LiteralPath $resolvedPublishDirectory -Recurse -Force
    }
}

function Invoke-Publish {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectPath
    )

    & dotnet publish $ProjectPath "-p:PublishProfile=win-x64"

    if ($LASTEXITCODE -ne 0) {
        throw "Publishing '$ProjectPath' failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repositoryRoot

try {
    Invoke-Publish "src\MyMediaImport.App\MyMediaImport.App.csproj"
    Invoke-Publish "src\MyMediaImport.Cli\MyMediaImport.Cli.csproj"

    $archivePath = Join-Path $publishRoot "MyMediaImport-$version.zip"
    $temporaryArchivePath = Join-Path $publishRoot "MyMediaImport-$version.partial.zip"

    try {
        if (Test-Path -LiteralPath $temporaryArchivePath) {
            Remove-Item -LiteralPath $temporaryArchivePath -Force
        }

        Compress-Archive `
            -Path $publishDirectories `
            -DestinationPath $temporaryArchivePath `
            -CompressionLevel Optimal

        Move-Item -LiteralPath $temporaryArchivePath -Destination $archivePath -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporaryArchivePath) {
            Remove-Item -LiteralPath $temporaryArchivePath -Force
        }
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Publish completed:"
Write-Host "  $publishRoot\app\MyMediaImport.App.exe"
Write-Host "  $publishRoot\cli\MyMediaImport.Cli.exe"
Write-Host "  $archivePath"
