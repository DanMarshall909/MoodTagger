using MoodTagger.Core.Models;
using MoodTagger.Core.Utils;
using NAudio.Wave;

namespace MoodTagger.Core.Audio;

/// <summary>
///     Processes audio files to extract features for mood analysis
/// </summary>
public class AudioProcessor
{
    private readonly AppConfig _config;

    /// <summary>
    ///     Initializes a new instance of the AudioProcessor class
    /// </summary>
    /// <param name="config">Application configuration</param>
    public AudioProcessor(AppConfig config) => _config = config ?? throw new ArgumentNullException(nameof(config));

    /// <summary>
    ///     Extracts audio features from an MP3 file
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted audio features</returns>
    public async Task<AudioFeatures> ExtractFeaturesAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");

        // Create a temporary object to hold feature values
        float bpm = 0;
        var metadata = new Dictionary<string, string>();

        // Extract metadata using TagLib#
        await Task.Run(() => ExtractMetadata(filePath, metadata, ref bpm), cancellationToken);

        // Process audio using NAudio
        var audioFeatures = await Task.Run(() => ProcessAudio(filePath, metadata, bpm), cancellationToken);

        return audioFeatures;
    }

    /// <summary>
    ///     Extracts metadata from an MP3 file
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="metadata">Dictionary to store metadata</param>
    /// <param name="bpm">BPM value to populate</param>
    private void ExtractMetadata(string filePath, Dictionary<string, string> metadata, ref float bpm)
    {
        try
        {
            using var file = TagLib.File.Create(filePath);
            var tags = file.Tag;

            if (!string.IsNullOrEmpty(tags.Title))
                metadata["Title"] = tags.Title;

            if (!string.IsNullOrEmpty(tags.FirstPerformer))
                metadata["Artist"] = tags.FirstPerformer;

            if (!string.IsNullOrEmpty(tags.Album))
                metadata["Album"] = tags.Album;

            if (tags.Year > 0)
                metadata["Year"] = tags.Year.ToString();

            if (tags.Genres.Length > 0)
                metadata["Genre"] = string.Join(", ", tags.Genres);

            if (tags.BeatsPerMinute > 0)
                bpm = tags.BeatsPerMinute;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting metadata: {ex.Message}");
            // Continue processing even if metadata extraction fails
        }
    }

    /// <summary>
    ///     Processes audio to extract features
    /// </summary>
    /// <param name="filePath">Path to the MP3 file</param>
    /// <param name="metadata">Metadata dictionary</param>
    /// <param name="initialBpm">Initial BPM from metadata</param>
    /// <returns>Audio features</returns>
    private AudioFeatures ProcessAudio(string filePath, Dictionary<string, string> metadata, float initialBpm)
    {
        // Variables to hold feature values
        float bpm = initialBpm;
        float rmsEnergy = 0;
        float zeroCrossingRate = 0;
        float spectralCentroid = 0;
        float spectralFlux = 0;
        float spectralRolloff = 0;
        float spectralFlatness = 0;
        float bassPresence = 0;
        float midPresence = 0;
        float highPresence = 0;
        float rhythmStrength = 0;
        float rhythmRegularity = 0;
        float onsetDensity = 0;
        float[] waveformData = Array.Empty<float>();
        float[] spectralData = Array.Empty<float>();
        float[] mfccCoefficients = Array.Empty<float>();
        float[] beatHistogram = Array.Empty<float>();
        float[] energyEnvelope = Array.Empty<float>();

        try
        {
            using var reader = new AudioFileReader(filePath);
            // Resample if needed
            var resampler = new MediaFoundationResampler(reader, _config.SampleRate);

            // Read audio data
            var buffer = new float[reader.Length / sizeof(float)];
            var byteBuffer = new byte[buffer.Length * sizeof(float)];
            int bytesRead = resampler.Read(byteBuffer, 0, byteBuffer.Length);
            int samplesRead = bytesRead / sizeof(float);

            // Convert byte buffer to float buffer
            Buffer.BlockCopy(byteBuffer, 0, buffer, 0, bytesRead);

            // Resize buffer if needed
            if (samplesRead < buffer.Length) Array.Resize(ref buffer, samplesRead);

            // Store waveform data (downsampled for efficiency)
            waveformData = DownsampleArray(buffer, 10000);

            // Calculate basic features
            CalculateBasicFeatures(buffer, ref rmsEnergy, ref zeroCrossingRate, ref energyEnvelope,
                ref bassPresence, ref midPresence, ref highPresence);

            // Calculate spectral features
            CalculateSpectralFeatures(buffer, ref spectralCentroid, ref spectralFlux,
                ref spectralRolloff, ref spectralFlatness);

            // Calculate rhythm features
            CalculateRhythmFeatures(buffer, ref rhythmStrength, ref rhythmRegularity,
                ref onsetDensity, ref beatHistogram);

            // Detect BPM if not already set from metadata
            if (bpm <= 0)
            {
                bpm = DetectBpm(buffer, reader.WaveFormat.SampleRate);

                // Ensure we have a valid BPM
                if (bpm <= 0)
                {
                    // Use a default BPM based on the genre if available
                    if (metadata.TryGetValue("Genre", out string? genre))
                        bpm = EstimateBpmFromGenre(genre);
                    else
                        // Default to 128 BPM for electronic music
                        bpm = 128;
                }
            }

            // Initialize spectral data and MFCC coefficients
            spectralData = new float[1000]; // Placeholder
            mfccCoefficients = new float[13]; // Placeholder
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing audio: {ex.Message}");

            // Set default values if processing fails
            SetDefaultFeatures(ref bpm, ref rmsEnergy, ref zeroCrossingRate, ref spectralCentroid,
                ref spectralFlux, ref spectralRolloff, ref spectralFlatness,
                ref bassPresence, ref midPresence, ref highPresence,
                ref rhythmStrength, ref rhythmRegularity, ref onsetDensity,
                ref waveformData, ref spectralData, ref mfccCoefficients,
                ref beatHistogram, ref energyEnvelope);
        }

        // Create and return the AudioFeatures object with all properties initialized
        return new AudioFeatures
        {
            FilePath = filePath,
            Bpm = bpm,
            RmsEnergy = rmsEnergy,
            ZeroCrossingRate = zeroCrossingRate,
            SpectralCentroid = spectralCentroid,
            SpectralFlux = spectralFlux,
            SpectralRolloff = spectralRolloff,
            SpectralFlatness = spectralFlatness,
            BassPresence = bassPresence,
            MidPresence = midPresence,
            HighPresence = highPresence,
            RhythmStrength = rhythmStrength,
            RhythmRegularity = rhythmRegularity,
            OnsetDensity = onsetDensity,
            WaveformData = waveformData,
            SpectralData = spectralData,
            MfccCoefficients = mfccCoefficients,
            BeatHistogram = beatHistogram,
            EnergyEnvelope = energyEnvelope,
            Metadata = metadata
        };
    }

    /// <summary>
    ///     Estimates BPM based on genre
    /// </summary>
    /// <param name="genre">Genre string</param>
    /// <returns>Estimated BPM</returns>
    private float EstimateBpmFromGenre(string genre)
    {
        genre = genre.ToLowerInvariant();

        if (genre.Contains("house") || genre.Contains("dance") || genre.Contains("edm"))
            return 128;

        if (genre.Contains("techno"))
            return 130;

        if (genre.Contains("trance"))
            return 138;

        if (genre.Contains("drum and bass") || genre.Contains("dnb") || genre.Contains("jungle"))
            return 174;

        if (genre.Contains("dubstep"))
            return 140;

        if (genre.Contains("hip hop") || genre.Contains("rap"))
            return 95;

        if (genre.Contains("rock"))
            return 120;

        if (genre.Contains("metal"))
            return 140;

        if (genre.Contains("jazz"))
            return 100;

        if (genre.Contains("ambient") || genre.Contains("chill"))
            return 85;

        // Default for electronic music
        return 128;
    }

    /// <summary>
    ///     Calculates basic audio features
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="rmsEnergy">RMS energy output</param>
    /// <param name="zeroCrossingRate">Zero crossing rate output</param>
    /// <param name="energyEnvelope">Energy envelope output</param>
    /// <param name="bassPresence">Bass presence output</param>
    /// <param name="midPresence">Mid presence output</param>
    /// <param name="highPresence">High presence output</param>
    private void CalculateBasicFeatures(float[] buffer, ref float rmsEnergy, ref float zeroCrossingRate,
        ref float[] energyEnvelope, ref float bassPresence,
        ref float midPresence, ref float highPresence)
    {
        // Calculate RMS energy
        float sumSquared = 0;
        for (var i = 0; i < buffer.Length; i++) sumSquared += buffer[i] * buffer[i];
        rmsEnergy = (float)Math.Sqrt(sumSquared / buffer.Length);

        // Calculate zero-crossing rate
        var zeroCrossings = 0;
        for (var i = 1; i < buffer.Length; i++)
            if ((buffer[i] >= 0 && buffer[i - 1] < 0) || (buffer[i] < 0 && buffer[i - 1] >= 0))
                zeroCrossings++;

        zeroCrossingRate = (float)zeroCrossings / buffer.Length;

        // Calculate energy envelope
        var envelopeSize = 1000; // Adjust as needed
        energyEnvelope = new float[envelopeSize];
        int samplesPerEnvelopePoint = buffer.Length / envelopeSize;

        for (var i = 0; i < envelopeSize; i++)
        {
            int startIdx = i * samplesPerEnvelopePoint;
            int endIdx = Math.Min(startIdx + samplesPerEnvelopePoint, buffer.Length);

            float sum = 0;
            for (int j = startIdx; j < endIdx; j++) sum += Math.Abs(buffer[j]);

            energyEnvelope[i] = sum / (endIdx - startIdx);
        }

        // Calculate frequency band energy
        CalculateFrequencyBandEnergy(zeroCrossingRate, ref bassPresence, ref midPresence, ref highPresence);
    }

    /// <summary>
    ///     Calculates energy in different frequency bands
    /// </summary>
    /// <param name="zeroCrossingRate">Zero crossing rate</param>
    /// <param name="bassPresence">Bass presence output</param>
    /// <param name="midPresence">Mid presence output</param>
    /// <param name="highPresence">High presence output</param>
    private void CalculateFrequencyBandEnergy(float zeroCrossingRate, ref float bassPresence,
        ref float midPresence, ref float highPresence)
    {
        // This is a simplified version - in a real implementation, 
        // we would use FFT to calculate energy in different frequency bands

        // For now, use zero-crossing rate as a rough approximation
        // Higher zero-crossing rate generally means more high-frequency content

        bassPresence = 1.0f - zeroCrossingRate * 5; // Rough approximation
        bassPresence = Math.Max(0, Math.Min(1, bassPresence));

        midPresence = 1.0f - Math.Abs(zeroCrossingRate * 10 - 1); // Rough approximation
        midPresence = Math.Max(0, Math.Min(1, midPresence));

        highPresence = zeroCrossingRate * 5; // Rough approximation
        highPresence = Math.Max(0, Math.Min(1, highPresence));
    }

    /// <summary>
    ///     Calculates spectral features
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="spectralCentroid">Spectral centroid output</param>
    /// <param name="spectralFlux">Spectral flux output</param>
    /// <param name="spectralRolloff">Spectral rolloff output</param>
    /// <param name="spectralFlatness">Spectral flatness output</param>
    private void CalculateSpectralFeatures(float[] buffer, ref float spectralCentroid,
        ref float spectralFlux, ref float spectralRolloff,
        ref float spectralFlatness)
    {
        // In a real implementation, we would use FFT to calculate spectral features
        // For this simplified version, we'll use approximations

        // Calculate zero-crossing rate for approximation
        var zeroCrossings = 0;
        for (var i = 1; i < buffer.Length; i++)
            if ((buffer[i] >= 0 && buffer[i - 1] < 0) || (buffer[i] < 0 && buffer[i - 1] >= 0))
                zeroCrossings++;

        float zcr = (float)zeroCrossings / buffer.Length;

        // Spectral centroid approximation based on zero-crossing rate
        spectralCentroid = zcr * 10000;

        // Spectral flux approximation
        spectralFlux = CalculateSpectralFluxApproximation(buffer);

        // Spectral rolloff approximation
        spectralRolloff = zcr * 15000;

        // Spectral flatness approximation
        spectralFlatness = 0.5f; // Default value
    }

    /// <summary>
    ///     Calculates an approximation of spectral flux
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <returns>Spectral flux approximation</returns>
    private float CalculateSpectralFluxApproximation(float[] buffer)
    {
        // Calculate average difference between consecutive samples
        float sum = 0;
        for (var i = 1; i < buffer.Length; i++) sum += Math.Abs(buffer[i] - buffer[i - 1]);

        return sum / (buffer.Length - 1);
    }

    /// <summary>
    ///     Calculates rhythm features
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="rhythmStrength">Rhythm strength output</param>
    /// <param name="rhythmRegularity">Rhythm regularity output</param>
    /// <param name="onsetDensity">Onset density output</param>
    /// <param name="beatHistogram">Beat histogram output</param>
    private void CalculateRhythmFeatures(float[] buffer, ref float rhythmStrength,
        ref float rhythmRegularity, ref float onsetDensity,
        ref float[] beatHistogram)
    {
        // In a real implementation, we would use more sophisticated algorithms
        // For this simplified version, we'll use approximations

        // Calculate onset detection function
        float[] onsetFunction = CalculateOnsetFunction(buffer);

        // Calculate rhythm strength based on variance of onset function
        rhythmStrength = CalculateVariance(onsetFunction);

        // Calculate rhythm regularity based on autocorrelation of onset function
        rhythmRegularity = CalculateRhythmRegularity(onsetFunction);

        // Calculate onset density
        onsetDensity = CalculateOnsetDensity(onsetFunction);

        // Create beat histogram (simplified)
        beatHistogram = new float[100]; // 100 BPM bins from 60-160 BPM
        for (var i = 0; i < beatHistogram.Length; i++)
        {
            float bpm = 60 + i;
            float distance = Math.Abs(bpm - 120); // Assuming 120 BPM as default
            beatHistogram[i] = (float)Math.Exp(-distance * distance / 100);
        }
    }

    /// <summary>
    ///     Calculates an onset detection function
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <returns>Onset detection function</returns>
    private float[] CalculateOnsetFunction(float[] buffer)
    {
        // Simplified onset detection function based on amplitude changes
        var windowSize = 1024;
        var hopSize = 512;
        int numFrames = (buffer.Length - windowSize) / hopSize + 1;

        var onsetFunction = new float[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            int startIdx = i * hopSize;
            float energy = 0;

            for (var j = 0; j < windowSize; j++)
                if (startIdx + j < buffer.Length)
                    energy += buffer[startIdx + j] * buffer[startIdx + j];

            onsetFunction[i] = energy;
        }

        // Calculate difference
        for (int i = numFrames - 1; i > 0; i--) onsetFunction[i] = Math.Max(0, onsetFunction[i] - onsetFunction[i - 1]);
        onsetFunction[0] = 0;

        return onsetFunction;
    }

    /// <summary>
    ///     Calculates variance of an array
    /// </summary>
    /// <param name="array">Input array</param>
    /// <returns>Variance</returns>
    private float CalculateVariance(float[] array)
    {
        float mean = array.Average();
        float sumSquaredDiff = 0;

        for (var i = 0; i < array.Length; i++)
        {
            float diff = array[i] - mean;
            sumSquaredDiff += diff * diff;
        }

        return sumSquaredDiff / array.Length;
    }

    /// <summary>
    ///     Calculates rhythm regularity based on autocorrelation
    /// </summary>
    /// <param name="onsetFunction">Onset detection function</param>
    /// <returns>Rhythm regularity measure</returns>
    private float CalculateRhythmRegularity(float[] onsetFunction)
    {
        // Simplified rhythm regularity based on autocorrelation
        int maxLag = onsetFunction.Length / 3;
        var autocorr = new float[maxLag];

        for (var lag = 0; lag < maxLag; lag++)
        {
            float sum = 0;
            for (var i = 0; i < onsetFunction.Length - lag; i++) sum += onsetFunction[i] * onsetFunction[i + lag];
            autocorr[lag] = sum;
        }

        // Normalize
        if (autocorr[0] > 0)
            for (var i = 0; i < maxLag; i++)
                autocorr[i] /= autocorr[0];

        // Find peaks
        var peaks = new List<int>();
        for (var i = 1; i < maxLag - 1; i++)
            if (autocorr[i] > autocorr[i - 1] && autocorr[i] > autocorr[i + 1])
                peaks.Add(i);

        // Calculate regularity based on peak heights
        float regularity = 0;
        if (peaks.Count > 0)
        {
            float sumPeakHeights = 0;
            foreach (int peak in peaks) sumPeakHeights += autocorr[peak];
            regularity = sumPeakHeights / peaks.Count;
        }

        return regularity;
    }

    /// <summary>
    ///     Calculates onset density
    /// </summary>
    /// <param name="onsetFunction">Onset detection function</param>
    /// <returns>Onset density measure</returns>
    private float CalculateOnsetDensity(float[] onsetFunction)
    {
        // Count onsets (peaks in onset function)
        var onsetCount = 0;
        float threshold = onsetFunction.Average() * 1.5f;

        for (var i = 1; i < onsetFunction.Length - 1; i++)
            if (onsetFunction[i] > threshold &&
                onsetFunction[i] > onsetFunction[i - 1] &&
                onsetFunction[i] > onsetFunction[i + 1])
                onsetCount++;

        // Calculate density (onsets per second)
        // Assuming 44100 Hz sample rate, 512 hop size
        float durationSeconds = onsetFunction.Length * 512 / 44100f;
        return onsetCount / durationSeconds;
    }

    /// <summary>
    ///     Detects BPM from audio buffer
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="sampleRate">Sample rate</param>
    /// <returns>Detected BPM</returns>
    private float DetectBpm(float[] buffer, int sampleRate)
    {
        try
        {
            // Simplified BPM detection
            // In a real implementation, we would use more sophisticated algorithms

            // Calculate onset function
            float[] onsetFunction = CalculateOnsetFunction(buffer);

            if (onsetFunction.Length < 2) return 0; // Not enough data

            // Calculate autocorrelation
            var minBpm = 60;
            var maxBpm = 180;
            int minLag = sampleRate * 60 / (maxBpm * 512); // Convert BPM to lag in frames
            int maxLag = sampleRate * 60 / (minBpm * 512);
            maxLag = Math.Min(maxLag, onsetFunction.Length / 2);

            if (minLag >= maxLag || minLag < 1) return 0; // Invalid lag range

            var autocorr = new float[maxLag - minLag + 1];

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float sum = 0;
                for (var i = 0; i < onsetFunction.Length - lag; i++) sum += onsetFunction[i] * onsetFunction[i + lag];
                autocorr[lag - minLag] = sum;
            }

            // Find peak
            var peakLag = 0;
            float peakValue = 0;

            for (var i = 0; i < autocorr.Length; i++)
                if (autocorr[i] > peakValue)
                {
                    peakValue = autocorr[i];
                    peakLag = i + minLag;
                }

            if (peakLag == 0) return 0; // No peak found

            // Convert lag to BPM
            float bpm = sampleRate * 60f / (peakLag * 512);

            // Ensure BPM is in reasonable range
            if (bpm < minBpm || bpm > maxBpm) return 0; // BPM outside reasonable range

            return bpm;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in BPM detection: {ex.Message}");
            return 0; // Return 0 to indicate failure
        }
    }

    /// <summary>
    ///     Downsamples an array to a specified length
    /// </summary>
    /// <param name="input">Input array</param>
    /// <param name="targetLength">Target length</param>
    /// <returns>Downsampled array</returns>
    private float[] DownsampleArray(float[] input, int targetLength)
    {
        if (input.Length <= targetLength) return input;

        var output = new float[targetLength];
        double step = (double)input.Length / targetLength;

        for (var i = 0; i < targetLength; i++)
        {
            var idx = (int)(i * step);
            output[i] = input[idx];
        }

        return output;
    }

    /// <summary>
    ///     Sets default feature values when processing fails
    /// </summary>
    private void SetDefaultFeatures(ref float bpm, ref float rmsEnergy, ref float zeroCrossingRate,
        ref float spectralCentroid, ref float spectralFlux,
        ref float spectralRolloff, ref float spectralFlatness,
        ref float bassPresence, ref float midPresence, ref float highPresence,
        ref float rhythmStrength, ref float rhythmRegularity, ref float onsetDensity,
        ref float[] waveformData, ref float[] spectralData, ref float[] mfccCoefficients,
        ref float[] beatHistogram, ref float[] energyEnvelope)
    {
        // Set default values for essential features
        if (bpm <= 0) bpm = 128; // Default to 128 BPM for electronic music
        rmsEnergy = 0.5f;
        zeroCrossingRate = 0.1f;
        spectralCentroid = 1000;
        spectralFlux = 0.1f;
        spectralRolloff = 5000;
        spectralFlatness = 0.5f;
        bassPresence = 0.5f;
        midPresence = 0.5f;
        highPresence = 0.5f;
        rhythmStrength = 0.5f;
        rhythmRegularity = 0.5f;
        onsetDensity = 1.0f;

        // Create default arrays
        waveformData = new float[1000];
        spectralData = new float[1000];
        mfccCoefficients = new float[13];
        beatHistogram = new float[100];
        energyEnvelope = new float[1000];
    }
}