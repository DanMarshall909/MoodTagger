using MoodTagger.Core.AI;
using MoodTagger.Core.Audio;
using MoodTagger.Core.Models;
using MoodTagger.Core.Tags;
using MoodTagger.Core.Utils;

namespace MoodTagger.Core;

/// <summary>
///     Main class for analyzing MP3 files and adding mood tags
/// </summary>
public class MoodAnalyzer
{
    private readonly AudioProcessor _audioProcessor;
    private readonly AppConfig _config;
    private readonly OllamaClient _ollamaClient;
    private readonly TagManager _tagManager;

    /// <summary>
    ///     Initializes a new instance of the MoodAnalyzer class
    /// </summary>
    /// <param name="config">Application configuration</param>
    public MoodAnalyzer(AppConfig? config = null)
    {
        _config = config ?? new AppConfig();
        _audioProcessor = new AudioProcessor(_config);
        _ollamaClient = new OllamaClient(_config);
        _tagManager = new TagManager(_config);
    }

    /// <summary>
    ///     Analyzes an MP3 file and returns the mood analysis
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mood analysis results</returns>
    public async Task<MoodAnalysis> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");

        // Check if file is already analyzed
        var existingAnalysis = await _tagManager.ReadTagsAsync(filePath, cancellationToken);
        if (existingAnalysis != null && !string.IsNullOrEmpty(existingAnalysis.ModelUsed))
        {
            if (_config.VerboseOutput)
            {
                Console.WriteLine($"File already analyzed: {filePath}");
                Console.WriteLine($"Previous analysis: {existingAnalysis}");
            }

            return existingAnalysis;
        }

        // Extract audio features
        if (_config.VerboseOutput) Console.WriteLine($"Extracting features from {filePath}...");

        var features = await _audioProcessor.ExtractFeaturesAsync(filePath, cancellationToken);

        // Analyze mood using Ollama
        if (_config.VerboseOutput) Console.WriteLine($"Analyzing mood with {_config.OllamaModel}...");

        var analysis = await _ollamaClient.AnalyzeMoodAsync(features, cancellationToken);

        // Validate analysis
        if (!analysis.ValidateRanges())
            Console.WriteLine($"Warning: Analysis for {filePath} contains values outside expected ranges");

        // Write tags if not in test mode
        if (_config.WriteTags)
        {
            if (_config.VerboseOutput) Console.WriteLine($"Writing tags to {filePath}...");

            await _tagManager.WriteTagsAsync(analysis, cancellationToken);
        }
        else if (_config.VerboseOutput)
        {
            Console.WriteLine($"Test mode: Not writing tags to {filePath}");
        }

        return analysis;
    }

    /// <summary>
    ///     Analyzes multiple MP3 files in batch
    /// </summary>
    /// <param name="filePaths">Paths to the MP3 files</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of file paths to mood analysis results</returns>
    public async Task<Dictionary<string, MoodAnalysis>> BatchProcessAsync(
        IEnumerable<string> filePaths,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, MoodAnalysis>();
        var fileList = filePaths.ToList();
        var batchProgress = new BatchProgress
        {
            Total = fileList.Count,
            Completed = 0,
            Failed = 0,
            StartTime = DateTime.Now
        };

        for (var i = 0; i < fileList.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            string filePath = fileList[i];

            try
            {
                var analysis = await AnalyzeFileAsync(filePath, cancellationToken);
                results[filePath] = analysis;
                batchProgress.Completed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                batchProgress.Failed++;
            }

            // Update progress
            batchProgress.ElapsedTime = DateTime.Now - batchProgress.StartTime;

            if (batchProgress.Completed > 0)
            {
                double timePerFile = batchProgress.ElapsedTime.TotalSeconds / batchProgress.Completed;
                int remainingFiles = batchProgress.Total - batchProgress.Completed - batchProgress.Failed;
                batchProgress.EstimatedTimeRemaining = TimeSpan.FromSeconds(timePerFile * remainingFiles);
            }

            progress?.Report(batchProgress);
        }

        return results;
    }

    /// <summary>
    ///     Processes a directory of MP3 files
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <param name="recursive">Whether to process subdirectories</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of file paths to mood analysis results</returns>
    public async Task<Dictionary<string, MoodAnalysis>> ProcessDirectoryAsync(
        string directoryPath,
        bool recursive = false,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(directoryPath, "*.mp3", searchOption);

        if (_config.VerboseOutput) Console.WriteLine($"Found {files.Length} MP3 files in {directoryPath}");

        return await BatchProcessAsync(files, progress, cancellationToken);
    }

    /// <summary>
    ///     Writes tags to an MP3 file
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="analysis">Mood analysis results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> WriteTagsAsync(string filePath, MoodAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));

        // Update file path in case it's different
        analysis.FilePath = filePath;

        return await _tagManager.WriteTagsAsync(analysis, cancellationToken);
    }

    /// <summary>
    ///     Reads tags from an MP3 file
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mood analysis results, or null if not found</returns>
    public async Task<MoodAnalysis> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default) =>
        await _tagManager.ReadTagsAsync(filePath, cancellationToken) ??
        throw new FileNotFoundException($"Tags not found for file: {filePath}");

    /// <summary>
    ///     Restores a backup of an MP3 file
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> RestoreBackupAsync(string filePath, CancellationToken cancellationToken = default) =>
        await _tagManager.RestoreBackupAsync(filePath, cancellationToken);
}