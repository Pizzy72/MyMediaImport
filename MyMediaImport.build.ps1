param(
    [string] $BuildMode = "Release",
    [string] $Platform = "x64",
    [string[]] $CliArguments = @("help")
)

$SolutionPath = "MyMediaImport.sln"
$AppProjectPath = "src\MyMediaImport.App\MyMediaImport.App.csproj"
$CliProjectPath = "src\MyMediaImport.Cli\MyMediaImport.Cli.csproj"

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

# Synopsis: Publish the desktop application and CLI for 64-bit Windows.
task Publish {
    exec { & "scripts\Publish.ps1" }
}

# Synopsis: Test and publish MyMediaImport.
task Release Test, Publish

task . Build
