using System.Numerics;
using System.Text.Json.Serialization;
using CsvHelper.Configuration.Attributes;

namespace AudioAnalyzer.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    public string FilePath { get; init; } = String.Empty;
    public double? TotalTime { get; set; }
    public int? BeatsPerMinute { get; set; }
    public double? AverageZeroCrossingRate { get; set; }
    public double? AverageRootMeanSquare { get; set; }
    public float? AverageEnvelope { get; set; }
    internal double TimeStep { get; set; }
    internal double FrequencyStep { get; set; }
    internal List<Complex[]> Spectrogram { get; set; } = new();
    public double? AverageBandEnergyRatio { get; set; }
    public double? AverageSpectralCentroid { get; set; }
    // Psychoacoustic features
    //MFCCs reflect the nature of the sound
    [Ignore]
    [JsonIgnore]
    public List<double[]> MFCC { get; set; } = new();

}