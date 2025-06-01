namespace MoodTagger.Core.Models
{
    /// <summary>
    /// Represents audio features extracted from an MP3 file
    /// </summary>
    public record AudioFeatures
    {
        /// <summary>
        /// The file path of the MP3
        /// </summary>
        public string FilePath { get; init; } = string.Empty;

        /// <summary>
        /// Beats per minute
        /// </summary>
        public float Bpm { get; init; }

        /// <summary>
        /// Waveform data (downsampled for visualization)
        /// </summary>
        public float[] WaveformData { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Spectral data (frequency domain representation)
        /// </summary>
        public float[] SpectralData { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Root mean square energy
        /// </summary>
        public float RmsEnergy { get; init; }

        /// <summary>
        /// Zero-crossing rate
        /// </summary>
        public float ZeroCrossingRate { get; init; }

        /// <summary>
        /// Spectral centroid
        /// </summary>
        public float SpectralCentroid { get; init; }

        /// <summary>
        /// Spectral flux
        /// </summary>
        public float SpectralFlux { get; init; }

        /// <summary>
        /// Spectral rolloff
        /// </summary>
        public float SpectralRolloff { get; init; }

        /// <summary>
        /// Spectral flatness
        /// </summary>
        public float SpectralFlatness { get; init; }

        /// <summary>
        /// Bass presence (low frequency energy)
        /// </summary>
        public float BassPresence { get; init; }

        /// <summary>
        /// Mid-range presence (mid frequency energy)
        /// </summary>
        public float MidPresence { get; init; }

        /// <summary>
        /// High-range presence (high frequency energy)
        /// </summary>
        public float HighPresence { get; init; }

        /// <summary>
        /// Rhythm strength
        /// </summary>
        public float RhythmStrength { get; init; }

        /// <summary>
        /// Rhythm regularity
        /// </summary>
        public float RhythmRegularity { get; init; }

        /// <summary>
        /// Onset density (number of note onsets per second)
        /// </summary>
        public float OnsetDensity { get; init; }

        /// <summary>
        /// MFCC coefficients
        /// </summary>
        public float[] MfccCoefficients { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Beat histogram (distribution of beat strengths at different tempos)
        /// </summary>
        public float[] BeatHistogram { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Energy envelope over time
        /// </summary>
        public float[] EnergyEnvelope { get; init; } = Array.Empty<float>();

        /// <summary>
        /// Additional metadata from the file
        /// </summary>
        public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }
}
