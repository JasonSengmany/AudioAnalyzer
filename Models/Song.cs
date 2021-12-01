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

    /// <value>Total running time of the song in seconds</value>
    public double? TotalTime { get; set; }

    /// <value>Estimate of the song's beats per minute set by class <c>BeatDetector</c></value>
    public int? BeatsPerMinute { get; set; }

    private List<double> _zeroCrossingRates = new();

    /// <value>Zero crossing rate for each frame partitioned by class <c>ZeroCrossingRateExtractor</c></value>
    [JsonIgnore]
    [Ignore]
    public List<double> ZeroCrossingRates
    {
        get { return _zeroCrossingRates; }
        set
        {
            _zeroCrossingRates = value;
            AverageZeroCrossingRate = _zeroCrossingRates.Average();
        }
    }
    public double? AverageZeroCrossingRate { get; set; }
    private List<double> _rootMeanSquares = new();

    [JsonIgnore]
    [Ignore]
    public List<double> RootMeanSquares
    {
        get { return _rootMeanSquares; }
        set
        {
            _rootMeanSquares = value;
            AverageRootMeanSquare = _rootMeanSquares.Average();
        }
    }
    public double? AverageRootMeanSquare { get; set; }
    private List<float> _amplitudeEnvelope = new();

    [JsonIgnore]
    [Ignore]
    public List<float> AmplitudeEnvelope
    {
        get { return _amplitudeEnvelope; }
        set
        {
            _amplitudeEnvelope = value;
            AverageEnvelope = _amplitudeEnvelope.Average();
        }
    }

    public float? AverageEnvelope { get; set; }
    internal double TimeStep { get; set; }
    internal double FrequencyStep { get; set; }

    [JsonIgnore]
    [Ignore]
    public List<Complex[]> Spectrogram { get; set; } = new();
    private List<double> _bandEnergyRatios = new();
    [JsonIgnore]
    [Ignore]
    public List<double> BandEnergyRatios
    {
        get { return _bandEnergyRatios; }
        set
        {
            _bandEnergyRatios = value;
            AverageBandEnergyRatio = _bandEnergyRatios.Average();
        }
    }
    public double? AverageBandEnergyRatio { get; set; }
    private List<double> _spectralCentroids = new();

    [JsonIgnore]
    [Ignore]
    public List<double> SpectralCentroids
    {
        get { return _spectralCentroids; }
        set
        {
            _spectralCentroids = value;
            AverageSpectralCentroid = _spectralCentroids.Average();
        }
    }
    public double? AverageSpectralCentroid { get; set; }

    // Psychoacoustic features
    //MFCCs reflect the nature of the sound
    [Ignore]
    public List<double[]> MFCC { get; set; } = new();

    // public Song(string filePath) => FilePath = filePath;


}