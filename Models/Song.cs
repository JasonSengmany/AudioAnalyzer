using AudioAnalyzer.FeatureExtraction;
using CsvHelper.Configuration.Attributes;

namespace AudioAnalyzer.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    internal string FilePath { get; init; } = String.Empty;

    [FeatureExtractors(nameof(DirectoryLabelExtractor), nameof(CustomLabelExtractor))]
    public string Label { get; set; } = String.Empty;

    [FeatureExtractors(nameof(TimeSpanExtractor))]
    public double? TotalTime { get; set; }

    [Optional]
    [FeatureExtractors(nameof(CombFilterBeatDetector))]
    public int? BeatsPerMinute { get; set; }

    [Optional]
    [FeatureExtractors(nameof(ZeroCrossingRateExtractor))]
    public double? AverageZeroCrossingRate { get; set; }

    [Optional]
    [FeatureExtractors(nameof(ZeroCrossingRateExtractor))]
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
    public double[] MFCC { get; set; } = new double[0];

    /// <summary>
    /// Used to pass variables between extractors
    /// </summary>
    internal Dictionary<string, dynamic> _metadata = new();
}