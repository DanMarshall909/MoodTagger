using CommandLine;

namespace MoodTagger.CLI;

[Verb("config", HelpText = "Manage configuration")]
public record ConfigOptions
{
    [Option("create", Required = false, HelpText = "Create default configuration")]
    public bool Create { get; init; }

    [Option("show", Required = false, HelpText = "Show current configuration")]
    public bool Show { get; init; }

    [Option('c', "config", Required = false, HelpText = "Path to the configuration file")]
    public string? ConfigPath { get; init; }

    [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
    public bool Verbose { get; init; }
}