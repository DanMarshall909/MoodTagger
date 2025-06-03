namespace MoodTagger.Core;

/// <summary>
///     Progress information for batch processing
/// </summary>
public class BatchProgress
{
    /// <summary>
    ///     Total number of files to process
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     Number of files completed
    /// </summary>
    public int Completed { get; set; }

    /// <summary>
    ///     Number of files that failed
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    ///     Start time of the batch process
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///     Elapsed time since the start
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    ///     Estimated time remaining
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; set; }

    /// <summary>
    ///     Percentage of completion
    /// </summary>
    public double PercentComplete => Total > 0 ? (double)(Completed + Failed) / Total * 100 : 0;
}