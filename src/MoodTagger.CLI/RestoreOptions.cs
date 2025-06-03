using CommandLine;

namespace MoodTagger.CLI;

[Verb("restore", HelpText = "Restore a backup of an MP3 file")]
public record RestoreOptions
{
    [Option('f', "file", Required = true, HelpText = "Path to the MP3 file")]
    public string FilePath { get; init; } = string.Empty;

    [Option('c', "config", Required = false, HelpText = "Path to the configuration file")]
    public string? ConfigPath { get; init; }

    [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
    public bool Verbose { get; init; }
}