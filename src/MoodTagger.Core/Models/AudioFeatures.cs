namespace MoodTagger.Core.Models
{
    /// <summary>
    /// Represents audio features extracted from an MP3 file
    /// </summary>
    public class AudioFeatures
    {
        /// <summary>
        /// The file path of the MP3
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Beats per minute
        /// </summary>
        public float Bpm { get; set; }

        /// <summary>
        /// Waveform data (downsampled for visualization)
        /// </summary>
        public float[] WaveformData { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Spectral data (frequency domain representation)
        /// </summary>
        public float[] SpectralData { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Root mean square energy
        /// </summary>
        public float RmsEnergy { get; set; }

        /// <summary>
        /// Zero-crossing rate
        /// </summary>
        public float ZeroCrossingRate { get; set; }

        /// <summary>
        /// Spectral centroid
        /// </summary>
        public float SpectralCentroid { get; set; }

        /// <summary>
        /// Spectral flux
        /// </summary>
        public float SpectralFlux { get; set; }

        /// <summary>
        /// Spectral rolloff
        /// </summary>
        public float SpectralRolloff { get; set; }

        /// <summary>
        /// Spectral flatness
        /// </summary>
        public float SpectralFlatness { get; set; }

        /// <summary>
        /// Bass presence (low frequency energy)
        /// </summary>
        public float BassPresence { get; set; }

        /// <summary>
        /// Mid-range presence (mid frequency energy)
        /// </summary>
        public float MidPresence { get; set; }

        /// <summary>
        /// High-range presence (high frequency energy)
        /// </summary>
        public float HighPresence { get; set; }

        /// <summary>
        /// Rhythm strength
        /// </summary>
        public float RhythmStrength { get; set; }

        /// <summary>
        /// Rhythm regularity
        /// </summary>
        public float RhythmRegularity { get; set; }

        /// <summary>
        /// Onset density (number of note onsets per second)
        /// </summary>
        public float OnsetDensity { get; set; }

        /// <summary>
        /// MFCC coefficients
        /// </summary>
        public float[] MfccCoefficients { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Beat histogram (distribution of beat strengths at different tempos)
        /// </summary>
        public float[] BeatHistogram { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Energy envelope over time
        /// </summary>
        public float[] EnergyEnvelope { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Additional metadata from the file
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
