using CommandLine;

namespace MoodTagger.CLI;

[Verb("batch", HelpText = "Process a directory of MP3 files")]
public record BatchOptions
{
    [Option('d', "dir", Required = true, HelpText = "Path to the directory")]
    public string DirectoryPath { get; init; } = string.Empty;

    [Option('r', "recursive", Required = false, HelpText = "Process subdirectories")]
    public bool Recursive { get; init; }

    [Option('c', "config", Required = false, HelpText = "Path to the configuration file")]
    public string? ConfigPath { get; init; }

    [Option('t', "test", Required = false, HelpText = "Test mode (don't write tags)")]
    public bool TestMode { get; init; }

    [Option('n', "no-backup", Required = false, HelpText = "Don't create backups")]
    public bool NoBackup { get; init; }

    [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
    public bool Verbose { get; init; }
}