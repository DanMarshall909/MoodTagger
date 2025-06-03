namespace MoodTagger.Core.Models;

/// <summary>
///     Represents the mood analysis results for an MP3 file
/// </summary>
public record MoodAnalysis
{
    /// <summary>
    ///     The file path of the analyzed MP3
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Mood Valence (-5 to +5): Emotional tone. -5 = very dark, 0 = neutral, +5 = euphoric
    /// </summary>
    public float MoodValence { get; set; }

    /// <summary>
    ///     Energy (1 to 10): Intensity & drive. 1 = ambient, 10 = rave monster
    /// </summary>
    public float Energy { get; set; }

    /// <summary>
    ///     Groove Tightness (-5 to +5): -5 = highly swung/broken, 0 = straight, +5 = extremely tight quantized
    /// </summary>
    public float GrooveTightness { get; set; }

    /// <summary>
    ///     Funk / Swing (0 to 10): Groove funkiness. More swing, syncopation = higher score
    /// </summary>
    public float FunkSwing { get; set; }

    /// <summary>
    ///     Tempo (Exact BPM): e.g. 128, 140, 172
    /// </summary>
    public float Tempo { get; set; }

    /// <summary>
    ///     Dancefloor Use (1 to 5): 1 = ambient opener, 3 = peak groover, 5 = main drop bomb
    /// </summary>
    public float DancefloorUse { get; set; }

    /// <summary>
    ///     Layering Potential (0 to 10): How well it layers over others; e.g., tool tracks score high
    /// </summary>
    public float LayeringPotential { get; set; }

    /// <summary>
    ///     Tension (-5 to +5): -5 = deeply relaxing, +5 = anxiety-inducing / suspenseful
    /// </summary>
    public float Tension { get; set; }

    /// <summary>
    ///     Rhythmic Complexity (0 to 10): Polyrhythms, syncopation, odd time = high score
    /// </summary>
    public float RhythmicComplexity { get; set; }

    /// <summary>
    ///     Sound Palette (-5 to +5): -5 = organic/acoustic, +5 = synthetic/futuristic
    /// </summary>
    public float SoundPalette { get; set; }

    /// <summary>
    ///     Explanations for each mood dimension
    /// </summary>
    public Dictionary<string, string> Explanations { get; set; } = new();

    /// <summary>
    ///     Timestamp when the analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.Now;

    /// <summary>
    ///     The model used for analysis
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    ///     Validates that all mood values are within their expected ranges
    /// </summary>
    /// <returns>True if all values are valid, false otherwise</returns>
    public bool ValidateRanges() =>
        MoodValence >= -5 && MoodValence <= 5 &&
        Energy >= 1 && Energy <= 10 &&
        GrooveTightness >= -5 && GrooveTightness <= 5 &&
        FunkSwing >= 0 && FunkSwing <= 10 &&
        Tempo > 0 &&
        DancefloorUse >= 1 && DancefloorUse <= 5 &&
        LayeringPotential >= 0 && LayeringPotential <= 10 &&
        Tension >= -5 && Tension <= 5 &&
        RhythmicComplexity >= 0 && RhythmicComplexity <= 10 &&
        SoundPalette >= -5 && SoundPalette <= 5;

    /// <summary>
    ///     Returns a summary of the mood analysis
    /// </summary>
    /// <returns>A string containing a summary of the mood analysis</returns>
    public override string ToString() =>
        $"Mood Analysis for {Path.GetFileName(FilePath)}:\n" +
        $"  Mood Valence: {MoodValence:F1} (-5 to +5)\n" +
        $"  Energy: {Energy:F1} (1 to 10)\n" +
        $"  Groove Tightness: {GrooveTightness:F1} (-5 to +5)\n" +
        $"  Funk/Swing: {FunkSwing:F1} (0 to 10)\n" +
        $"  Tempo: {Tempo:F1} BPM\n" +
        $"  Dancefloor Use: {DancefloorUse:F1} (1 to 5)\n" +
        $"  Layering Potential: {LayeringPotential:F1} (0 to 10)\n" +
        $"  Tension: {Tension:F1} (-5 to +5)\n" +
        $"  Rhythmic Complexity: {RhythmicComplexity:F1} (0 to 10)\n" +
        $"  Sound Palette: {SoundPalette:F1} (-5 to +5)\n" +
        $"  Analyzed with: {ModelUsed} at {AnalysisTimestamp}";
}