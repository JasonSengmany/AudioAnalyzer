using System.Numerics;
using System.Text.Json.Serialization;

namespace AudioAnalyzer.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    [JsonInclude]
    public string FilePath { get; init; } = String.Empty;

    /// <value>Total running time of the song in seconds</value>
    [JsonInclude]
    public double? TotalTime { get; internal set; }

    /// <value>Estimate of the song's beats per minute set by class <c>BeatDetector</c></value>
    [JsonInclude]
    public int? BeatsPerMinute { get; internal set; }

    /// <value>Zero crossing rate for each frame partitioned by class <c>ZeroCrossingRateExtractor</c></value>
    public List<double> ZeroCrossingRates { get; internal set; }

    [JsonInclude]
    public double? AverageZeroCrossingRate => ZeroCrossingRates?.Average();
    public List<double> RootMeanSquares { get; internal set; }

    [JsonInclude]
    public double? AverageRootMeanSquare => RootMeanSquares?.Average();
    public List<float> AmplitudeEnvelope { get; internal set; }

    [JsonInclude]
    public float? AverageEnvelope => AmplitudeEnvelope?.Average();


    internal double TimeStep { get; set; }
    internal double FrequencyStep { get; set; }
    public List<Complex[]> Spectrogram { get; internal set; }

    [JsonInclude]
    public List<double> BandEnergyRatios { get; internal set; }
    // Vocals typically have < 100 Average BER while electronic music have > 120
    public double? AverageBandEnergyRatio => BandEnergyRatios?.Average();

    public List<double> SpectralCentroids { get; internal set; }

    [JsonInclude]
    public double? AverageSpectralCentroid => SpectralCentroids?.Average();

    // Psychoacoustic features
    //MFCCs reflect the nature of the sound
    [JsonInclude]
    public List<double[]> MFCC { get; internal set; }

    public Song(string filePath) => FilePath = filePath;


}