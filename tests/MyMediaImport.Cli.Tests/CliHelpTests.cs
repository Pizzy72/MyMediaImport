using MyMediaImport.Cli;

namespace MyMediaImport.Cli.Tests;

[TestClass]
public sealed class CliHelpTests
{
    private static readonly string[] Commands =
        ["devices", "list", "import", "import-one", "inspect", "time-test", "help"];

    [TestMethod]
    public void GeneralHelp_ContainsRequiredSectionsAndEveryCommand()
    {
        StringAssert.Contains(CliHelp.General, "Usage:");
        StringAssert.Contains(CliHelp.General, "Commands:");
        StringAssert.Contains(CliHelp.General, "Options:");
        StringAssert.Contains(CliHelp.General, "Examples:");
        Assert.IsFalse(CliHelp.General.Contains("Syntax (EBNF):", StringComparison.Ordinal));

        foreach (string command in Commands)
        {
            StringAssert.Contains(CliHelp.General, command);
        }

        Assert.IsFalse(CliHelp.General.Contains("import-option", StringComparison.Ordinal));
        Assert.IsFalse(CliHelp.General.Contains("time-range", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EveryCommand_HasCommandSpecificHelp()
    {
        foreach (string command in Commands)
        {
            Assert.IsTrue(CliHelp.TryGetCommandHelp(command, out string? help), command);
            StringAssert.Contains(help, "Usage:");
            StringAssert.Contains(help, "Options:");
            StringAssert.Contains(help, "Examples:");
            StringAssert.Contains(help, "For the formal syntax:");
            StringAssert.Contains(help, $"help {command} syntax");
            Assert.IsFalse(help.Contains("Syntax (EBNF):", StringComparison.Ordinal));

            Assert.IsTrue(CliHelp.TryGetCommandSyntax(command, out string? syntax), command);
            StringAssert.Contains(syntax, "Usage:");
            StringAssert.Contains(syntax, "Syntax (EBNF):");
            Assert.IsFalse(syntax.Contains("Options:", StringComparison.Ordinal));
            Assert.IsFalse(syntax.Contains("Examples:", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void ImportHelp_MatchesImplementedOptionsAndTimeRanges()
    {
        string help = CliHelp.Import;
        string[] expectedLiterals =
        [
            "--device-index", "--today", "--yesterday", "--last",
            "--from", "--to", "--all", "--timezone", "--target",
            "--extension", "--skip-existing", "--rename-existing", "--overwrite"
        ];

        foreach (string literal in expectedLiterals)
        {
            StringAssert.Contains(help, literal);
        }

        StringAssert.Contains(help, "{capture:FORMAT}");
        StringAssert.Contains(help, "{captureUtc:FORMAT}");
        StringAssert.Contains(help, "{original}");
        StringAssert.Contains(help, "{ext}");
        StringAssert.Contains(help, "{collision:FORMAT}");
        StringAssert.Contains(help, "Validation:");
        StringAssert.Contains(help, "no daylight-saving rules");
        StringAssert.Contains(
            help,
            "Exactly one --device-index, exactly one time selection,");
        Assert.IsFalse(help.Contains(
            "the last occurrence supplies the effective value",
            StringComparison.Ordinal));
        Assert.IsFalse(help.Contains("[ \".\" ]", StringComparison.Ordinal));

        string syntax = CliHelp.ImportSyntax;
        StringAssert.Contains(syntax, "import-command = \"import\", { import-option }");
        StringAssert.Contains(syntax, "import-option");
        StringAssert.Contains(syntax, "date-range-option");
        Assert.IsFalse(syntax.Contains("Validation:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProductiveCommandHelp_SeparatesGrammarFromValidationRules()
    {
        StringAssert.Contains(CliHelp.List, "Validation:");
        StringAssert.Contains(CliHelp.Import, "Validation:");
        StringAssert.Contains(CliHelp.ImportOne, "Validation:");
    }

    [TestMethod]
    public void AllHelpOutput_ContainsAsciiOnly()
    {
        string[] outputs = new[]
        {
            CliHelp.General,
            CliHelp.Devices,
            CliHelp.List,
            CliHelp.Import,
            CliHelp.ImportOne,
            CliHelp.Inspect,
            CliHelp.TimeTest,
            CliHelp.Help,
            CliHelp.ImportSyntax
        };

        foreach (string? output in outputs)
        {
            Assert.IsFalse(
                output.Any(character => character > 127),
                "Help output contains a non-ASCII character.");
        }
    }

    [TestMethod]
    public void UnknownCommand_HasNoCommandSpecificHelp()
    {
        Assert.IsFalse(CliHelp.TryGetCommandHelp("unknown", out string? help));
        Assert.AreEqual(string.Empty, help);
    }
}
