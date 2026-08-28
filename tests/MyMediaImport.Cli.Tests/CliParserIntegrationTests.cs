using System.Diagnostics;

namespace MyMediaImport.Cli.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CliParserIntegrationTests
{
    [TestMethod]
    public async Task Help_SeparatesGeneralPracticalAndFormalViews()
    {
        CliProcessResult general = await RunCliAsync(["help"]);
        CliProcessResult practical = await RunCliAsync(["help", "import"]);
        CliProcessResult syntax = await RunCliAsync(["help", "import", "syntax"]);

        Assert.AreEqual(0, general.ExitCode);
        Assert.AreEqual(0, practical.ExitCode);
        Assert.AreEqual(0, syntax.ExitCode);
        Assert.IsFalse(general.StandardOutput.Contains("Syntax (EBNF):", StringComparison.Ordinal));
        Assert.IsFalse(practical.StandardOutput.Contains("Syntax (EBNF):", StringComparison.Ordinal));
        StringAssert.Contains(practical.StandardOutput, "For the formal syntax:");
        StringAssert.Contains(syntax.StandardOutput, "Syntax (EBNF):");
        Assert.IsFalse(syntax.StandardOutput.Contains("Options:", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("--device-index", "2", "--device-index may only be specified once.")]
    [DataRow("--timezone", "+02:00", "--timezone may only be specified once.")]
    [DataRow("--target", @"C:\Temp\other.jpg", "--target may only be specified once.")]
    public async Task Import_RejectsRepeatedRequiredOption(
        string option,
        string value,
        string expectedError)
    {
        List<string> arguments = ValidImportArguments();
        arguments.Add(option);
        arguments.Add(value);

        CliProcessResult result = await RunCliAsync(arguments);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardError, expectedError);
    }

    [TestMethod]
    public async Task Import_RejectsRepeatedExtension()
    {
        List<string> arguments = ValidImportArguments();
        arguments.AddRange(["--extension", "JPG", "--extension", "HEIC"]);

        CliProcessResult result = await RunCliAsync(arguments);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardError, "--extension may only be specified once.");
    }

    [TestMethod]
    public async Task Import_RejectsMultipleExistingFilePolicies()
    {
        List<string> arguments = ValidImportArguments();
        arguments.AddRange(["--skip-existing", "--rename-existing"]);

        CliProcessResult result = await RunCliAsync(arguments);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardError, "Use at most one of");
    }

    [TestMethod]
    [DataRow("list", "--device-index", "1", "2")]
    [DataRow("list", "--extension", "JPG", "HEIC")]
    [DataRow("import-one", "--device-index", "1", "2")]
    [DataRow("import-one", "--target", @"C:\Temp\one.jpg", @"C:\Temp\two.jpg")]
    [DataRow("inspect", "--device-index", "1", "2")]
    [DataRow("inspect", "--extension", "JPG", "HEIC")]
    public async Task OtherCommands_RejectRepeatedSingleOccurrenceOption(
        string command,
        string option,
        string firstValue,
        string secondValue)
    {
        CliProcessResult result = await RunCliAsync(
            [command, option, firstValue, option, secondValue]);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(
            result.StandardError,
            $"{option} may only be specified once.");
    }

    [TestMethod]
    [DataRow("list", ".JPG")]
    [DataRow("import", ".JPG")]
    [DataRow("import", "JPG,.HEIC")]
    [DataRow("inspect", ".JPG")]
    public async Task ExtensionFilter_RejectsLeadingDot(
        string command,
        string extensionList)
    {
        CliProcessResult result = await RunCliAsync(
            [command, "--extension", extensionList]);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardError, "without a leading dot");
    }

    [TestMethod]
    [DataRow("--today", "--yesterday")]
    [DataRow("--today", "--from", "2026-08-23")]
    [DataRow("--last", "3d", "--all")]
    [DataRow("--from", "2026-08-22", "--from", "2026-08-23")]
    [DataRow("--to", "2026-08-22", "--to", "2026-08-23")]
    public async Task Import_RejectsInvalidTimeSelection(params string[] timeArguments)
    {
        List<string> arguments = RequiredImportArgumentsWithoutTime();
        arguments.AddRange(timeArguments);

        CliProcessResult result = await RunCliAsync(arguments);

        Assert.AreNotEqual(0, result.ExitCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.StandardError));
    }

    [TestMethod]
    public async Task Import_RejectsMissingTimeSelection()
    {
        CliProcessResult result = await RunCliAsync(RequiredImportArgumentsWithoutTime());

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardError, "Use exactly one time selection");
    }

    private static List<string> ValidImportArguments()
    {
        List<string> arguments = RequiredImportArgumentsWithoutTime();
        arguments.Add("--today");
        return arguments;
    }

    private static List<string> RequiredImportArgumentsWithoutTime() =>
    [
        "import",
        "--device-index", "1",
        "--timezone", "local",
        "--target", @"C:\Temp\target.jpg"
    ];

    private static async Task<CliProcessResult> RunCliAsync(IEnumerable<string> arguments)
    {
        string cliAssembly = Path.Combine(AppContext.BaseDirectory, "MyMediaImport.Cli.dll");
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(cliAssembly);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the CLI test process.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new CliProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private sealed record CliProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
