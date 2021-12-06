using System.Numerics;
using CsvHelper.Configuration.Attributes;

namespace AudioAnalyzer.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    public string FilePath { get; init; } = String.Empty;
    public string Label { get; set; } = String.Empty;

    public double? TotalTime { get; set; }

    [Optional]
    public int? BeatsPerMinute { get; set; }

    [Optional]
    public double? AverageZeroCrossingRate { get; set; }

    [Optional]
    public double? AverageRootMeanSquare { get; set; }

    [Optional]
    public float? AverageEnvelope { get; set; }

    [Optional]
    public double? AverageBandEnergyRatio { get; set; }

    [Optional]
    public double? AverageSpectralCentroid { get; set; }

    [Optional]
    public double? AverageBandwidth { get; set; }

    [Optional]
    public double[] MFCC { get; set; } = new double[0];

    /// <summary>
    /// Used to pass variables between extractors
    /// </summary>
    internal Dictionary<string, dynamic> _metadata = new();
}