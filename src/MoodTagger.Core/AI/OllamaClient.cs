using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MoodTagger.Core.Models;
using MoodTagger.Core.Utils;

namespace MoodTagger.Core.AI;

/// <summary>
///     Client for interacting with the Ollama API
/// </summary>
public class OllamaClient
{
    private readonly HttpClient _client;
    private readonly AppConfig _config;
    private readonly string _generateEndpoint;

    /// <summary>
    ///     Initializes a new instance of the OllamaClient class
    /// </summary>
    /// <param name="config">Application configuration</param>
    public OllamaClient(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _client = new HttpClient();
        _generateEndpoint = $"{_config.OllamaBaseUrl.TrimEnd('/')}/generate";
    }

    /// <summary>
    ///     Analyzes audio features to determine mood
    /// </summary>
    /// <param name="features">Audio features to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mood analysis results</returns>
    public async Task<MoodAnalysis> AnalyzeMoodAsync(AudioFeatures features,
        CancellationToken cancellationToken = default)
    {
        if (features == null) throw new ArgumentNullException(nameof(features));

        string prompt = GeneratePrompt(features);
        string response = await GenerateCompletionAsync(prompt, cancellationToken);

        return ParseResponse(response, features.FilePath, features.Bpm);
    }

    /// <summary>
    ///     Generates a prompt for the Ollama API based on audio features
    /// </summary>
    /// <param name="features">Audio features</param>
    /// <returns>A prompt string</returns>
    private string GeneratePrompt(AudioFeatures features)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "You are an expert music analyzer. Analyze the following audio features and rate them according to the specified scales:");
        sb.AppendLine();
        sb.AppendLine("[Audio Features]");
        sb.AppendLine($"- BPM: {features.Bpm:F1}");
        sb.AppendLine($"- Spectral Centroid: {features.SpectralCentroid:F2}");
        sb.AppendLine($"- Spectral Flux: {features.SpectralFlux:F2}");
        sb.AppendLine($"- Rhythm Strength: {features.RhythmStrength:F2}");
        sb.AppendLine($"- Bass Presence: {features.BassPresence:F2}");
        sb.AppendLine($"- Mid Presence: {features.MidPresence:F2}");
        sb.AppendLine($"- High Presence: {features.HighPresence:F2}");
        sb.AppendLine($"- RMS Energy: {features.RmsEnergy:F2}");
        sb.AppendLine($"- Zero Crossing Rate: {features.ZeroCrossingRate:F2}");
        sb.AppendLine($"- Rhythm Regularity: {features.RhythmRegularity:F2}");
        sb.AppendLine();

        sb.AppendLine("Rate each dimension:");
        sb.AppendLine("1. Mood Valence (-5 to +5): Emotional tone. -5 = very dark, 0 = neutral, +5 = euphoric");
        sb.AppendLine("2. Energy (1 to 10): Intensity & drive. 1 = ambient, 10 = rave monster");
        sb.AppendLine(
            "3. Groove Tightness (-5 to +5): -5 = highly swung/broken, 0 = straight, +5 = extremely tight quantized");
        sb.AppendLine("4. Funk/Swing (0 to 10): Groove funkiness. More swing, syncopation = higher score");
        sb.AppendLine(
            "5. Tempo (BPM): Exact BPM value. If the provided BPM is 0 or seems incorrect, estimate a reasonable BPM based on other features");
        sb.AppendLine("6. Dancefloor Use (1 to 5): 1 = ambient opener, 3 = peak groover, 5 = main drop bomb");
        sb.AppendLine("7. Layering Potential (0 to 10): How well it layers over others; e.g., tool tracks score high");
        sb.AppendLine("8. Tension (-5 to +5): -5 = deeply relaxing, +5 = anxiety-inducing / suspenseful");
        sb.AppendLine("9. Rhythmic Complexity (0 to 10): Polyrhythms, syncopation, odd time = high score");
        sb.AppendLine("10. Sound Palette (-5 to +5): -5 = organic/acoustic, +5 = synthetic/futuristic");
        sb.AppendLine();
        sb.AppendLine("For each dimension, provide a rating and a brief explanation. Format your response as follows:");
        sb.AppendLine("Mood Valence: [rating] - [explanation]");
        sb.AppendLine("Energy: [rating] - [explanation]");
        sb.AppendLine("Groove Tightness: [rating] - [explanation]");
        sb.AppendLine("Funk/Swing: [rating] - [explanation]");
        sb.AppendLine("Tempo: [BPM] - [explanation]");
        sb.AppendLine("Dancefloor Use: [rating] - [explanation]");
        sb.AppendLine("And so on for each dimension.");
        sb.AppendLine();
        sb.AppendLine("The track is: " + Path.GetFileName(features.FilePath));

        if (features.Metadata.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Additional track metadata:");
            foreach (var meta in features.Metadata) sb.AppendLine($"- {meta.Key}: {meta.Value}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Sends a request to the Ollama API to generate a completion
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated completion</returns>
    private async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _config.OllamaModel,
            prompt,
            stream = false,
            options = new
            {
                temperature = _config.Temperature,
                num_predict = _config.MaxTokens
            }
        };

        string json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(_generateEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        string responseJson = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseJson);
        var root = jsonDoc.RootElement;

        return root.GetProperty("response").GetString() ?? string.Empty;
    }

    /// <summary>
    ///     Parses the response from the Ollama API into a MoodAnalysis object
    /// </summary>
    /// <param name="response">The response from the Ollama API</param>
    /// <param name="filePath">The file path of the analyzed MP3</param>
    /// <param name="detectedBpm">The BPM detected from audio analysis</param>
    /// <returns>A MoodAnalysis object</returns>
    private MoodAnalysis ParseResponse(string response, string filePath, float detectedBpm)
    {
        var analysis = new MoodAnalysis
        {
            FilePath = filePath,
            ModelUsed = _config.OllamaModel
        };

        // Extract ratings using regex
        ExtractRating(response, @"Mood Valence:\s*([-+]?\d+\.?\d*)", out float moodValence);
        analysis.MoodValence = moodValence;

        ExtractRating(response, @"Energy:\s*(\d+\.?\d*)", out float energy);
        analysis.Energy = energy;

        ExtractRating(response, @"Groove Tightness:\s*([-+]?\d+\.?\d*)", out float grooveTightness);
        analysis.GrooveTightness = grooveTightness;

        ExtractRating(response, @"Funk/Swing:\s*(\d+\.?\d*)", out float funkSwing);
        analysis.FunkSwing = funkSwing;

        // Extract tempo from response, or use detected BPM if available
        if (ExtractRating(response, @"Tempo:\s*(\d+\.?\d*)", out float tempo) && tempo > 0)
            analysis.Tempo = tempo;
        else if (detectedBpm > 0)
            analysis.Tempo = detectedBpm;
        else
            // Default to a reasonable BPM if all else fails
            analysis.Tempo = 120;

        ExtractRating(response, @"Dancefloor Use:\s*(\d+\.?\d*)", out float dancefloorUse);
        analysis.DancefloorUse = dancefloorUse;

        ExtractRating(response, @"Layering Potential:\s*(\d+\.?\d*)", out float layeringPotential);
        analysis.LayeringPotential = layeringPotential;

        ExtractRating(response, @"Tension:\s*([-+]?\d+\.?\d*)", out float tension);
        analysis.Tension = tension;

        ExtractRating(response, @"Rhythmic Complexity:\s*(\d+\.?\d*)", out float rhythmicComplexity);
        analysis.RhythmicComplexity = rhythmicComplexity;

        ExtractRating(response, @"Sound Palette:\s*([-+]?\d+\.?\d*)", out float soundPalette);
        analysis.SoundPalette = soundPalette;

        // Extract explanations
        ExtractExplanation(response, "Mood Valence", analysis.Explanations);
        ExtractExplanation(response, "Energy", analysis.Explanations);
        ExtractExplanation(response, "Groove Tightness", analysis.Explanations);
        ExtractExplanation(response, "Funk/Swing", analysis.Explanations);
        ExtractExplanation(response, "Tempo", analysis.Explanations);
        ExtractExplanation(response, "Dancefloor Use", analysis.Explanations);
        ExtractExplanation(response, "Layering Potential", analysis.Explanations);
        ExtractExplanation(response, "Tension", analysis.Explanations);
        ExtractExplanation(response, "Rhythmic Complexity", analysis.Explanations);
        ExtractExplanation(response, "Sound Palette", analysis.Explanations);

        return analysis;
    }

    /// <summary>
    ///     Extracts a rating from a response using regex
    /// </summary>
    /// <param name="response">The response text</param>
    /// <param name="pattern">The regex pattern to match</param>
    /// <param name="value">The extracted value</param>
    /// <returns>True if a value was extracted, false otherwise</returns>
    private bool ExtractRating(string response, string pattern, out float value)
    {
        var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1) return float.TryParse(match.Groups[1].Value, out value);

        value = 0;
        return false;
    }

    /// <summary>
    ///     Extracts an explanation for a mood dimension
    /// </summary>
    /// <param name="response">The response text</param>
    /// <param name="dimension">The mood dimension</param>
    /// <param name="explanations">Dictionary to store the explanation</param>
    private void ExtractExplanation(string response, string dimension, Dictionary<string, string> explanations)
    {
        var pattern = $@"{dimension}:\s*[-+]?\d+\.?\d*\s*-\s*(.+?)(?=\n\w|$)";
        var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match.Success && match.Groups.Count > 1) explanations[dimension] = match.Groups[1].Value.Trim();
    }
}