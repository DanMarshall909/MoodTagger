﻿using System.Text.Json;
using CommandLine;
using MoodTagger.Core;
using MoodTagger.Core.Utils;

namespace MoodTagger.CLI;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<
                AnalyzeOptions,
                BatchOptions,
                ReadOptions,
                RestoreOptions,
                ConfigOptions>(args)
            .MapResult(
                (AnalyzeOptions opts) => AnalyzeFileAsync(opts),
                (BatchOptions opts) => BatchProcessAsync(opts),
                (ReadOptions opts) => ReadTagsAsync(opts),
                (RestoreOptions opts) => RestoreBackupAsync(opts),
                (ConfigOptions opts) => HandleConfigAsync(opts),
                errs => Task.FromResult(1));
    }

    private static async Task<int> AnalyzeFileAsync(AnalyzeOptions options)
    {
        try
        {
            var config = LoadConfig(options.ConfigPath);
            config.VerboseOutput = options.Verbose;
            config.WriteTags = !options.TestMode;
            config.CreateBackups = !options.NoBackup;

            var analyzer = new MoodAnalyzer(config);

            Console.WriteLine($"Analyzing file: {options.FilePath}");
            var analysis = await analyzer.AnalyzeFileAsync(options.FilePath);

            Console.WriteLine();
            Console.WriteLine(analysis.ToString());

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static async Task<int> BatchProcessAsync(BatchOptions options)
    {
        try
        {
            var config = LoadConfig(options.ConfigPath);
            config.VerboseOutput = options.Verbose;
            config.WriteTags = !options.TestMode;
            config.CreateBackups = !options.NoBackup;
            config.Recursive = options.Recursive;

            var analyzer = new MoodAnalyzer(config);
            var progress = new Progress<BatchProgress>(ReportProgress);
            var cts = new CancellationTokenSource();

            // Set up cancellation on Ctrl+C
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancelling...");
                cts.Cancel();
                e.Cancel = true;
            };

            Console.WriteLine($"Processing directory: {options.DirectoryPath}");
            Console.WriteLine($"Recursive: {options.Recursive}");
            Console.WriteLine($"Test mode: {options.TestMode}");
            Console.WriteLine();

            var results = await analyzer.ProcessDirectoryAsync(
                options.DirectoryPath,
                options.Recursive,
                progress,
                cts.Token);

            Console.WriteLine();
            Console.WriteLine($"Completed: {results.Count} files processed");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static async Task<int> ReadTagsAsync(ReadOptions options)
    {
        try
        {
            var config = LoadConfig(options.ConfigPath);
            config.VerboseOutput = options.Verbose;

            var analyzer = new MoodAnalyzer(config);

            Console.WriteLine($"Reading tags from: {options.FilePath}");
            var analysis = await analyzer.ReadTagsAsync(options.FilePath);

            if (analysis == null)
            {
                Console.WriteLine("No mood tags found in file.");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine(analysis.ToString());

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static async Task<int> RestoreBackupAsync(RestoreOptions options)
    {
        try
        {
            var config = LoadConfig(options.ConfigPath);
            config.VerboseOutput = options.Verbose;

            var analyzer = new MoodAnalyzer(config);

            Console.WriteLine($"Restoring backup for: {options.FilePath}");
            bool success = await analyzer.RestoreBackupAsync(options.FilePath);

            if (success)
            {
                Console.WriteLine("Backup restored successfully.");
                return 0;
            }

            Console.WriteLine("Failed to restore backup.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static Task<int> HandleConfigAsync(ConfigOptions options)
    {
        try
        {
            string configPath = options.ConfigPath ?? GetDefaultConfigPath();

            if (options.Create)
            {
                // Create default config
                var config = new AppConfig();
                config.SaveToFile(configPath);
                Console.WriteLine($"Created default configuration at: {configPath}");
            }
            else if (options.Show)
            {
                // Show current config
                if (File.Exists(configPath))
                {
                    var config = AppConfig.LoadFromFile(configPath);
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(config, jsonOptions);
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine($"Configuration file not found: {configPath}");
                    return Task.FromResult(1);
                }
            }
            else
            {
                Console.WriteLine("Please specify --create or --show");
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) Console.Error.WriteLine(ex.StackTrace);
            return Task.FromResult(1);
        }
    }

    private static void ReportProgress(BatchProgress progress)
    {
        Console.Write($"\rProcessed: {progress.Completed}/{progress.Total} " +
                      $"({progress.PercentComplete:F1}%) " +
                      $"Failed: {progress.Failed} " +
                      $"Elapsed: {FormatTimeSpan(progress.ElapsedTime)} " +
                      $"ETA: {FormatTimeSpan(progress.EstimatedTimeRemaining)}");
    }

    private static string FormatTimeSpan(TimeSpan timeSpan) =>
        timeSpan.TotalHours >= 1
            ? $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s"
            : $"{timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s";

    private static AppConfig LoadConfig(string configPath)
    {
        configPath = configPath ?? GetDefaultConfigPath();

        try
        {
            return AppConfig.CreateDefaultIfNotExists(configPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load configuration from {configPath}: {ex.Message}");
            Console.WriteLine("Using default configuration.");
            return new AppConfig();
        }
    }

    private static string GetDefaultConfigPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string configDir = Path.Combine(appDataPath, "MoodTagger");

        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

        return Path.Combine(configDir, "config.json");
    }
}