using System.ComponentModel.DataAnnotations;
using AudioAnalyzer.FeatureExtraction;
using CsvHelper.Configuration.Attributes;

namespace AudioAnalyzer.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    [Required]
    internal string FilePath { get; init; } = String.Empty;

    [FeatureExtractors(nameof(DirectoryLabelExtractor))]
    public string? Label { get; set; }

    [FeatureExtractors(nameof(TimeSpanExtractor))]
    public double? TotalTime { get; set; }

    [Optional]
    [FeatureExtractors(nameof(CombFilterBeatDetector), nameof(ClearRiceBeatDetector))]
    public int? BeatsPerMinute { get; set; }

    [Optional]
    [FeatureExtractors(nameof(ZeroCrossingRateExtractor))]
    public double? AverageZeroCrossingRate { get; set; }

    [Optional]
    [FeatureExtractors(nameof(RootMeanSquareExtractor))]
    public double? AverageRootMeanSquare { get; set; }

    [Optional]
    [FeatureExtractors(nameof(BasicEnvelopeDetector))]
    public float? AverageEnvelope { get; set; }

    [Optional]
    [FeatureExtractors(nameof(BandEnergyRatioExtractor))]
    public double? AverageBandEnergyRatio { get; set; }

    [Optional]
    [FeatureExtractors(nameof(SpectralCentroidExtractor))]
    public double? AverageSpectralCentroid { get; set; }

    [Optional]
    [FeatureExtractors(nameof(BandwidthExtractor))]
    public double? AverageBandwidth { get; set; }

    [Optional]
    [FeatureExtractors(nameof(MfccExtractor))]
    public double[]? MFCC { get; set; }

    /// <summary>
    /// Used to pass variables between extractors
    /// </summary>
    internal Dictionary<string, dynamic> _metadata = new();
}