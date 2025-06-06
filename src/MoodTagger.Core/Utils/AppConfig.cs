using System.Text.Json;

namespace MoodTagger.Core.Utils;

/// <summary>
///     Configuration settings for the MoodTagger application
/// </summary>
public record AppConfig
{
    /// <summary>
    ///     Loads configuration from a file
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>The loaded configuration</returns>
    public static AppConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Configuration file not found: {filePath}");

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    /// <summary>
    ///     Saves configuration to a file
    /// </summary>
    /// <param name="filePath">Path to save the configuration file</param>
    public void SaveToFile(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath ?? throw new ArgumentNullException(nameof(filePath)))!;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    ///     Creates a default configuration file if it doesn't exist
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>The configuration (either loaded or default)</returns>
    public static AppConfig CreateDefaultIfNotExists(string filePath)
    {
        if (File.Exists(filePath)) return LoadFromFile(filePath);

        var config = new AppConfig();
        config.SaveToFile(filePath);
        return config;
    }

    #region Audio Processing Settings

    /// <summary>
    ///     Sample rate for audio processing
    /// </summary>
    public int SampleRate { get; init; } = 44100;

    /// <summary>
    ///     Frame size for audio processing
    /// </summary>
    public int FrameSize { get; init; } = 1024;

    /// <summary>
    ///     Hop size for audio processing
    /// </summary>
    public int HopSize { get; init; } = 512;

    #endregion

    #region Ollama Settings

    /// <summary>
    ///     Base URL for the Ollama API
    /// </summary>
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434/api";

    /// <summary>
    ///     Model to use for analysis
    /// </summary>
    public string OllamaModel { get; init; } = "llama3";

    /// <summary>
    ///     Temperature for the Ollama API
    /// </summary>
    public float Temperature { get; init; } = 0.1f;

    /// <summary>
    ///     Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 1000;

    #endregion

    #region Resource Settings

    /// <summary>
    ///     Maximum GPU memory to use in MB
    /// </summary>
    public int MaxGpuMemoryMB { get; init; } = 6000;

    /// <summary>
    ///     Batch size for processing
    /// </summary>
    public int BatchSize { get; init; } = 1;

    /// <summary>
    ///     Whether to use dynamic batching
    /// </summary>
    public bool DynamicBatching { get; init; } = true;

    #endregion

    #region Tag Settings

    /// <summary>
    ///     Whether to create backups before modifying files
    /// </summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>
    ///     Whether to preserve existing tags
    /// </summary>
    public bool PreserveExistingTags { get; set; } = true;

    /// <summary>
    ///     Whether to write tags to files (false for testing)
    /// </summary>
    public bool WriteTags { get; set; } = true;

    #endregion

    #region Processing Settings

    /// <summary>
    ///     Whether to process directories recursively
    /// </summary>
    public bool Recursive { get; set; }

    /// <summary>
    ///     Whether to output verbose information
    /// </summary>
    public bool VerboseOutput { get; set; }

    #endregion
}