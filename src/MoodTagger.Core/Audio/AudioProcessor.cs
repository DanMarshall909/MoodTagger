using MoodTagger.Core.Models;
using MoodTagger.Core.Utils;
using NAudio.Wave;

namespace MoodTagger.Core.Audio
{
    /// <summary>
    /// Processes audio files to extract features for mood analysis
    /// </summary>
    public class AudioProcessor
    {
        private readonly AppConfig _config;

        /// <summary>
        /// Initializes a new instance of the AudioProcessor class
        /// </summary>
        /// <param name="config">Application configuration</param>
        public AudioProcessor(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Extracts audio features from an MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extracted audio features</returns>
        public async Task<AudioFeatures> ExtractFeaturesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var features = new AudioFeatures
            {
                FilePath = filePath
            };

            // Extract metadata using TagLib#
            await Task.Run(() => ExtractMetadata(filePath, features), cancellationToken);

            // Process audio using NAudio
            await Task.Run(() => ProcessAudio(filePath, features), cancellationToken);

            return features;
        }

        /// <summary>
        /// Extracts metadata from an MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="features">Audio features to populate</param>
        private void ExtractMetadata(string filePath, AudioFeatures features)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    var tags = file.Tag;

                    if (!string.IsNullOrEmpty(tags.Title))
                        features.Metadata["Title"] = tags.Title;

                    if (!string.IsNullOrEmpty(tags.FirstPerformer))
                        features.Metadata["Artist"] = tags.FirstPerformer;

                    if (!string.IsNullOrEmpty(tags.Album))
                        features.Metadata["Album"] = tags.Album;

                    if (tags.Year > 0)
                        features.Metadata["Year"] = tags.Year.ToString();

                    if (tags.Genres.Length > 0)
                        features.Metadata["Genre"] = string.Join(", ", tags.Genres);

                    if (tags.BeatsPerMinute > 0)
                        features.Bpm = tags.BeatsPerMinute;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting metadata: {ex.Message}");
                // Continue processing even if metadata extraction fails
            }
        }

        /// <summary>
        /// Processes audio to extract features
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="features">Audio features to populate</param>
        private void ProcessAudio(string filePath, AudioFeatures features)
        {
            try
            {
                using (var reader = new AudioFileReader(filePath))
                {
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
                    if (samplesRead < buffer.Length)
                    {
                        Array.Resize(ref buffer, samplesRead);
                    }
                    
                    // Store waveform data (downsampled for efficiency)
                    features.WaveformData = DownsampleArray(buffer, 10000);
                    
                    // Calculate basic features
                    CalculateBasicFeatures(buffer, features);
                    
                    // Calculate spectral features
                    CalculateSpectralFeatures(buffer, features);
                    
                    // Calculate rhythm features
                    CalculateRhythmFeatures(buffer, features);
                    
                    // Detect BPM if not already set from metadata
                    if (features.Bpm <= 0)
                    {
                        features.Bpm = DetectBpm(buffer, reader.WaveFormat.SampleRate);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing audio: {ex.Message}");
                
                // Set default values if processing fails
                SetDefaultFeatures(features);
            }
        }

        /// <summary>
        /// Calculates basic audio features
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="features">Audio features to populate</param>
        private void CalculateBasicFeatures(float[] buffer, AudioFeatures features)
        {
            // Calculate RMS energy
            float sumSquared = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sumSquared += buffer[i] * buffer[i];
            }
            features.RmsEnergy = (float)Math.Sqrt(sumSquared / buffer.Length);
            
            // Calculate zero-crossing rate
            int zeroCrossings = 0;
            for (int i = 1; i < buffer.Length; i++)
            {
                if ((buffer[i] >= 0 && buffer[i - 1] < 0) || (buffer[i] < 0 && buffer[i - 1] >= 0))
                {
                    zeroCrossings++;
                }
            }
            features.ZeroCrossingRate = (float)zeroCrossings / buffer.Length;
            
            // Calculate energy envelope
            int envelopeSize = 1000; // Adjust as needed
            features.EnergyEnvelope = new float[envelopeSize];
            int samplesPerEnvelopePoint = buffer.Length / envelopeSize;
            
            for (int i = 0; i < envelopeSize; i++)
            {
                int startIdx = i * samplesPerEnvelopePoint;
                int endIdx = Math.Min(startIdx + samplesPerEnvelopePoint, buffer.Length);
                
                float sum = 0;
                for (int j = startIdx; j < endIdx; j++)
                {
                    sum += Math.Abs(buffer[j]);
                }
                
                features.EnergyEnvelope[i] = sum / (endIdx - startIdx);
            }
            
            // Calculate frequency band energy
            CalculateFrequencyBandEnergy(buffer, features);
        }

        /// <summary>
        /// Calculates energy in different frequency bands
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="features">Audio features to populate</param>
        private void CalculateFrequencyBandEnergy(float[] buffer, AudioFeatures features)
        {
            // This is a simplified version - in a real implementation, 
            // we would use FFT to calculate energy in different frequency bands
            
            // For now, use zero-crossing rate as a rough approximation
            // Higher zero-crossing rate generally means more high-frequency content
            
            features.BassPresence = 1.0f - features.ZeroCrossingRate * 5; // Rough approximation
            features.BassPresence = Math.Max(0, Math.Min(1, features.BassPresence));
            
            features.MidPresence = 1.0f - Math.Abs(features.ZeroCrossingRate * 10 - 1); // Rough approximation
            features.MidPresence = Math.Max(0, Math.Min(1, features.MidPresence));
            
            features.HighPresence = features.ZeroCrossingRate * 5; // Rough approximation
            features.HighPresence = Math.Max(0, Math.Min(1, features.HighPresence));
        }

        /// <summary>
        /// Calculates spectral features
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="features">Audio features to populate</param>
        private void CalculateSpectralFeatures(float[] buffer, AudioFeatures features)
        {
            // In a real implementation, we would use FFT to calculate spectral features
            // For this simplified version, we'll use approximations
            
            // Spectral centroid approximation based on zero-crossing rate
            features.SpectralCentroid = features.ZeroCrossingRate * 10000;
            
            // Spectral flux approximation
            features.SpectralFlux = CalculateSpectralFluxApproximation(buffer);
            
            // Spectral rolloff approximation
            features.SpectralRolloff = features.ZeroCrossingRate * 15000;
            
            // Spectral flatness approximation
            features.SpectralFlatness = 0.5f; // Default value
        }

        /// <summary>
        /// Calculates an approximation of spectral flux
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <returns>Spectral flux approximation</returns>
        private float CalculateSpectralFluxApproximation(float[] buffer)
        {
            // Calculate average difference between consecutive samples
            float sum = 0;
            for (int i = 1; i < buffer.Length; i++)
            {
                sum += Math.Abs(buffer[i] - buffer[i - 1]);
            }
            
            return sum / (buffer.Length - 1);
        }

        /// <summary>
        /// Calculates rhythm features
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="features">Audio features to populate</param>
        private void CalculateRhythmFeatures(float[] buffer, AudioFeatures features)
        {
            // In a real implementation, we would use more sophisticated algorithms
            // For this simplified version, we'll use approximations
            
            // Calculate onset detection function
            float[] onsetFunction = CalculateOnsetFunction(buffer);
            
            // Calculate rhythm strength based on variance of onset function
            features.RhythmStrength = CalculateVariance(onsetFunction);
            
            // Calculate rhythm regularity based on autocorrelation of onset function
            features.RhythmRegularity = CalculateRhythmRegularity(onsetFunction);
            
            // Calculate onset density
            features.OnsetDensity = CalculateOnsetDensity(onsetFunction);
            
            // Create beat histogram (simplified)
            features.BeatHistogram = new float[100]; // 100 BPM bins from 60-160 BPM
            for (int i = 0; i < features.BeatHistogram.Length; i++)
            {
                float bpm = 60 + i;
                float distance = Math.Abs(bpm - features.Bpm);
                features.BeatHistogram[i] = (float)Math.Exp(-distance * distance / 100);
            }
        }

        /// <summary>
        /// Calculates an onset detection function
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <returns>Onset detection function</returns>
        private float[] CalculateOnsetFunction(float[] buffer)
        {
            // Simplified onset detection function based on amplitude changes
            int windowSize = 1024;
            int hopSize = 512;
            int numFrames = (buffer.Length - windowSize) / hopSize + 1;
            
            float[] onsetFunction = new float[numFrames];
            
            for (int i = 0; i < numFrames; i++)
            {
                int startIdx = i * hopSize;
                float energy = 0;
                
                for (int j = 0; j < windowSize; j++)
                {
                    if (startIdx + j < buffer.Length)
                    {
                        energy += buffer[startIdx + j] * buffer[startIdx + j];
                    }
                }
                
                onsetFunction[i] = energy;
            }
            
            // Calculate difference
            for (int i = numFrames - 1; i > 0; i--)
            {
                onsetFunction[i] = Math.Max(0, onsetFunction[i] - onsetFunction[i - 1]);
            }
            onsetFunction[0] = 0;
            
            return onsetFunction;
        }

        /// <summary>
        /// Calculates variance of an array
        /// </summary>
        /// <param name="array">Input array</param>
        /// <returns>Variance</returns>
        private float CalculateVariance(float[] array)
        {
            float mean = array.Average();
            float sumSquaredDiff = 0;
            
            for (int i = 0; i < array.Length; i++)
            {
                float diff = array[i] - mean;
                sumSquaredDiff += diff * diff;
            }
            
            return sumSquaredDiff / array.Length;
        }

        /// <summary>
        /// Calculates rhythm regularity based on autocorrelation
        /// </summary>
        /// <param name="onsetFunction">Onset detection function</param>
        /// <returns>Rhythm regularity measure</returns>
        private float CalculateRhythmRegularity(float[] onsetFunction)
        {
            // Simplified rhythm regularity based on autocorrelation
            int maxLag = onsetFunction.Length / 3;
            float[] autocorr = new float[maxLag];
            
            for (int lag = 0; lag < maxLag; lag++)
            {
                float sum = 0;
                for (int i = 0; i < onsetFunction.Length - lag; i++)
                {
                    sum += onsetFunction[i] * onsetFunction[i + lag];
                }
                autocorr[lag] = sum;
            }
            
            // Normalize
            if (autocorr[0] > 0)
            {
                for (int i = 0; i < maxLag; i++)
                {
                    autocorr[i] /= autocorr[0];
                }
            }
            
            // Find peaks
            List<int> peaks = new List<int>();
            for (int i = 1; i < maxLag - 1; i++)
            {
                if (autocorr[i] > autocorr[i - 1] && autocorr[i] > autocorr[i + 1])
                {
                    peaks.Add(i);
                }
            }
            
            // Calculate regularity based on peak heights
            float regularity = 0;
            if (peaks.Count > 0)
            {
                float sumPeakHeights = 0;
                foreach (int peak in peaks)
                {
                    sumPeakHeights += autocorr[peak];
                }
                regularity = sumPeakHeights / peaks.Count;
            }
            
            return regularity;
        }

        /// <summary>
        /// Calculates onset density
        /// </summary>
        /// <param name="onsetFunction">Onset detection function</param>
        /// <returns>Onset density measure</returns>
        private float CalculateOnsetDensity(float[] onsetFunction)
        {
            // Count onsets (peaks in onset function)
            int onsetCount = 0;
            float threshold = onsetFunction.Average() * 1.5f;
            
            for (int i = 1; i < onsetFunction.Length - 1; i++)
            {
                if (onsetFunction[i] > threshold && 
                    onsetFunction[i] > onsetFunction[i - 1] && 
                    onsetFunction[i] > onsetFunction[i + 1])
                {
                    onsetCount++;
                }
            }
            
            // Calculate density (onsets per second)
            // Assuming 44100 Hz sample rate, 512 hop size
            float durationSeconds = onsetFunction.Length * 512 / 44100f;
            return onsetCount / durationSeconds;
        }

        /// <summary>
        /// Detects BPM from audio buffer
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="sampleRate">Sample rate</param>
        /// <returns>Detected BPM</returns>
        private float DetectBpm(float[] buffer, int sampleRate)
        {
            // Simplified BPM detection
            // In a real implementation, we would use more sophisticated algorithms
            
            // Calculate onset function
            float[] onsetFunction = CalculateOnsetFunction(buffer);
            
            // Calculate autocorrelation
            int minBpm = 60;
            int maxBpm = 180;
            int minLag = sampleRate * 60 / (maxBpm * 512); // Convert BPM to lag in frames
            int maxLag = sampleRate * 60 / (minBpm * 512);
            maxLag = Math.Min(maxLag, onsetFunction.Length / 2);
            
            float[] autocorr = new float[maxLag - minLag + 1];
            
            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float sum = 0;
                for (int i = 0; i < onsetFunction.Length - lag; i++)
                {
                    sum += onsetFunction[i] * onsetFunction[i + lag];
                }
                autocorr[lag - minLag] = sum;
            }
            
            // Find peak
            int peakLag = 0;
            float peakValue = 0;
            
            for (int i = 0; i < autocorr.Length; i++)
            {
                if (autocorr[i] > peakValue)
                {
                    peakValue = autocorr[i];
                    peakLag = i + minLag;
                }
            }
            
            // Convert lag to BPM
            float bpm = sampleRate * 60f / (peakLag * 512);
            
            // Ensure BPM is in reasonable range
            if (bpm < minBpm || bpm > maxBpm)
            {
                bpm = 120; // Default BPM if detection fails
            }
            
            return bpm;
        }

        /// <summary>
        /// Downsamples an array to a specified length
        /// </summary>
        /// <param name="input">Input array</param>
        /// <param name="targetLength">Target length</param>
        /// <returns>Downsampled array</returns>
        private float[] DownsampleArray(float[] input, int targetLength)
        {
            if (input.Length <= targetLength)
            {
                return input;
            }
            
            float[] output = new float[targetLength];
            double step = (double)input.Length / targetLength;
            
            for (int i = 0; i < targetLength; i++)
            {
                int idx = (int)(i * step);
                output[i] = input[idx];
            }
            
            return output;
        }

        /// <summary>
        /// Sets default feature values when processing fails
        /// </summary>
        /// <param name="features">Audio features to populate</param>
        private void SetDefaultFeatures(AudioFeatures features)
        {
            // Set default values for essential features
            if (features.Bpm <= 0) features.Bpm = 120;
            features.RmsEnergy = 0.5f;
            features.ZeroCrossingRate = 0.1f;
            features.SpectralCentroid = 1000;
            features.SpectralFlux = 0.1f;
            features.SpectralRolloff = 5000;
            features.SpectralFlatness = 0.5f;
            features.BassPresence = 0.5f;
            features.MidPresence = 0.5f;
            features.HighPresence = 0.5f;
            features.RhythmStrength = 0.5f;
            features.RhythmRegularity = 0.5f;
            features.OnsetDensity = 1.0f;
            
            // Create default arrays
            features.WaveformData = new float[1000];
            features.SpectralData = new float[1000];
            features.MfccCoefficients = new float[13];
            features.BeatHistogram = new float[100];
            features.EnergyEnvelope = new float[1000];
        }
    }
}
