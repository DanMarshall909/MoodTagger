using MoodTagger.Core.Models;
using MoodTagger.Core.Utils;
using TagLib;
using TagLib.Id3v2;

namespace MoodTagger.Core.Tags
{
    /// <summary>
    /// Manages reading and writing tags to MP3 files
    /// </summary>
    public class TagManager
    {
        private readonly AppConfig _config;

        /// <summary>
        /// Initializes a new instance of the TagManager class
        /// </summary>
        /// <param name="config">Application configuration</param>
        public TagManager(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Writes mood analysis results to MP3 file tags
        /// </summary>
        /// <param name="analysis">Mood analysis results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> WriteTagsAsync(MoodAnalysis analysis, CancellationToken cancellationToken = default)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            if (!System.IO.File.Exists(analysis.FilePath))
            {
                throw new FileNotFoundException($"File not found: {analysis.FilePath}");
            }

            // Skip writing if in test mode
            if (!_config.WriteTags)
            {
                Console.WriteLine($"Test mode: Would write tags to {analysis.FilePath}");
                return true;
            }

            // Create backup if configured
            if (_config.CreateBackups)
            {
                await CreateBackupAsync(analysis.FilePath, cancellationToken);
            }

            return await Task.Run(() => WriteTagsInternal(analysis), cancellationToken);
        }

        /// <summary>
        /// Creates a backup of the MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task CreateBackupAsync(string filePath, CancellationToken cancellationToken)
        {
            string backupPath = filePath + ".bak";
            
            // Remove existing backup if it exists
            if (System.IO.File.Exists(backupPath))
            {
                System.IO.File.Delete(backupPath);
            }

            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);
        }

        /// <summary>
        /// Writes tags to the MP3 file
        /// </summary>
        /// <param name="analysis">Mood analysis results</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool WriteTagsInternal(MoodAnalysis analysis)
        {
            try
            {
                using var file = TagLib.File.Create(analysis.FilePath);
                var tag = file.GetTag(TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
                    
                if (tag == null)
                {
                    Console.WriteLine($"Error: Could not create ID3v2 tag for {analysis.FilePath}");
                    return false;
                }

                // Write mood tags as TXXX frames
                WriteCustomTag(tag, "MoodValence", analysis.MoodValence.ToString("F1"));
                WriteCustomTag(tag, "Energy", analysis.Energy.ToString("F1"));
                WriteCustomTag(tag, "GrooveTightness", analysis.GrooveTightness.ToString("F1"));
                WriteCustomTag(tag, "FunkSwing", analysis.FunkSwing.ToString("F1"));
                WriteCustomTag(tag, "DancefloorUse", analysis.DancefloorUse.ToString("F1"));
                WriteCustomTag(tag, "LayeringPotential", analysis.LayeringPotential.ToString("F1"));
                WriteCustomTag(tag, "Tension", analysis.Tension.ToString("F1"));
                WriteCustomTag(tag, "RhythmicComplexity", analysis.RhythmicComplexity.ToString("F1"));
                WriteCustomTag(tag, "SoundPalette", analysis.SoundPalette.ToString("F1"));
                    
                // Write BPM if detected
                if (analysis.Tempo > 0)
                {
                    tag.BeatsPerMinute = (uint)Math.Round(analysis.Tempo);
                }
                    
                // Write analysis timestamp and model
                WriteCustomTag(tag, "AnalysisTimestamp", analysis.AnalysisTimestamp.ToString("o"));
                WriteCustomTag(tag, "AnalysisModel", analysis.ModelUsed);
                    
                // Write explanations if available
                foreach (var explanation in analysis.Explanations)
                {
                    WriteCustomTag(tag, $"Explanation_{explanation.Key}", explanation.Value);
                }
                    
                // Save the file
                file.Save();
                    
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing tags to {analysis.FilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Writes a custom tag to the ID3v2 tag
        /// </summary>
        /// <param name="tag">ID3v2 tag</param>
        /// <param name="description">Tag description</param>
        /// <param name="value">Tag value</param>
        private void WriteCustomTag(TagLib.Id3v2.Tag tag, string description, string value)
        {
            // Check if the tag already exists
            UserTextInformationFrame? existingFrame = null;
            
            foreach (Frame frame in tag.GetFrames())
            {
                if (frame is UserTextInformationFrame textFrame && 
                    textFrame.Description == description)
                {
                    existingFrame = textFrame;
                    break;
                }
            }
            
            if (existingFrame != null)
            {
                // Update existing frame if preserving existing tags
                if (_config.PreserveExistingTags)
                {
                    existingFrame.Text = new[] { value };
                }
                else
                {
                    // Remove existing frame if not preserving
                    tag.RemoveFrame(existingFrame);
                    
                    // Create new frame
                    var newFrame = new UserTextInformationFrame(description)
                    {
                        Text = new[] { value }
                    };
                    tag.AddFrame(newFrame);
                }
            }
            else
            {
                // Create new frame
                var newFrame = new UserTextInformationFrame(description)
                {
                    Text = new[] { value }
                };
                tag.AddFrame(newFrame);
            }
        }

        /// <summary>
        /// Reads mood tags from an MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Mood analysis results, or null if not found</returns>
        public async Task<MoodAnalysis?> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return await Task.Run(() => ReadTagsInternal(filePath), cancellationToken);
        }

        /// <summary>
        /// Reads tags from the MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <returns>Mood analysis results, or null if not found</returns>
        private MoodAnalysis? ReadTagsInternal(string filePath)
        {
            try
            {
                using var file = TagLib.File.Create(filePath);
                var tag = file.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                    
                if (tag == null)
                {
                    return null;
                }

                var analysis = new MoodAnalysis
                {
                    FilePath = filePath,
                    // Read mood tags
                    MoodValence = ReadCustomTagFloat(tag, "MoodValence"),
                    Energy = ReadCustomTagFloat(tag, "Energy"),
                    GrooveTightness = ReadCustomTagFloat(tag, "GrooveTightness"),
                    FunkSwing = ReadCustomTagFloat(tag, "FunkSwing"),
                    DancefloorUse = ReadCustomTagFloat(tag, "DancefloorUse"),
                    LayeringPotential = ReadCustomTagFloat(tag, "LayeringPotential"),
                    Tension = ReadCustomTagFloat(tag, "Tension"),
                    RhythmicComplexity = ReadCustomTagFloat(tag, "RhythmicComplexity"),
                    SoundPalette = ReadCustomTagFloat(tag, "SoundPalette")
                };

                // Read BPM
                if (tag.BeatsPerMinute > 0)
                {
                    analysis.Tempo = tag.BeatsPerMinute;
                }
                    
                // Read analysis timestamp and model
                string timestampStr = ReadCustomTagString(tag, "AnalysisTimestamp");
                if (!string.IsNullOrEmpty(timestampStr) && DateTime.TryParse(timestampStr, out DateTime timestamp))
                {
                    analysis.AnalysisTimestamp = timestamp;
                }
                    
                analysis.ModelUsed = ReadCustomTagString(tag, "AnalysisModel");
                    
                // Read explanations
                foreach (Frame frame in tag.GetFrames())
                {
                    if (frame is UserTextInformationFrame textFrame && 
                        textFrame.Description.StartsWith("Explanation_") &&
                        textFrame.Text.Length > 0)
                    {
                        string key = textFrame.Description.Substring("Explanation_".Length);
                        analysis.Explanations[key] = textFrame.Text[0];
                    }
                }
                    
                return analysis;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading tags from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a custom tag as a float
        /// </summary>
        /// <param name="tag">ID3v2 tag</param>
        /// <param name="description">Tag description</param>
        /// <returns>Tag value as float, or 0 if not found</returns>
        private float ReadCustomTagFloat(TagLib.Id3v2.Tag tag, string description)
        {
            string value = ReadCustomTagString(tag, description);
            
            if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float result))
            {
                return result;
            }
            
            return 0;
        }

        /// <summary>
        /// Reads a custom tag as a string
        /// </summary>
        /// <param name="tag">ID3v2 tag</param>
        /// <param name="description">Tag description</param>
        /// <returns>Tag value as string, or empty if not found</returns>
        private string ReadCustomTagString(TagLib.Id3v2.Tag tag, string description)
        {
            foreach (Frame frame in tag.GetFrames())
            {
                if (frame is UserTextInformationFrame textFrame && 
                    textFrame.Description == description &&
                    textFrame.Text.Length > 0)
                {
                    return textFrame.Text[0];
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Restores a backup of the MP3 file
        /// </summary>
        /// <param name="filePath">Path to the MP3 file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RestoreBackupAsync(string filePath, CancellationToken cancellationToken = default)
        {
            string backupPath = filePath + ".bak";
            
            if (!System.IO.File.Exists(backupPath))
            {
                Console.WriteLine($"No backup found for {filePath}");
                return false;
            }
            
            try
            {
                // Delete the original file
                System.IO.File.Delete(filePath);
                
                // Copy the backup to the original path
                using var sourceStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring backup for {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}
