using CommandLine;

namespace MoodTagger.CLI;

[Verb("analyze", HelpText = "Analyze a single MP3 file")]
public record AnalyzeOptions
{
    [Option('f', "file", Required = true, HelpText = "Path to the MP3 file")]
    public string FilePath { get; init; } = string.Empty;

    [Option('c', "config", Required = false, HelpText = "Path to the configuration file")]
    public string? ConfigPath { get; init; }

    [Option('t', "test", Required = false, HelpText = "Test mode (don't write tags)")]
    public bool TestMode { get; init; }

    [Option('n', "no-backup", Required = false, HelpText = "Don't create backups")]
    public bool NoBackup { get; init; }

    [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
    public bool Verbose { get; init; }
}