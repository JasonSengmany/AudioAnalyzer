using System.Numerics;

namespace AudioAnalyser.Models;


/// <summary>
/// Model <c>Song</c> used to store all possible extracted features. 
/// </summary>
public record Song
{
    public string FilePath { get; init; } = String.Empty;

    /// <value>Total running time of the song</value>
    public TimeSpan TotalTime { get; internal set; }

    /// <value>Estimate of the song's beats per minute set by class <c>BeatDetector</c></value>
    public int BeatsPerMinute { get; internal set; }

    /// <value>Zero crossing rate for each frame partitioned by class <c>ZeroCrossingRateExtractor</c></value>
    public List<double> ZeroCrossingRates { get; internal set; }

    public double? AverageZeroCrossingRate => ZeroCrossingRates?.Average();
    public List<double> RootMeanSquares { get; internal set; }
    public double? AverageRootMeanSquare => RootMeanSquares?.Average();
    public List<float> AmplitudeEnvelope { get; internal set; }
    public float? AverageEnvelope => AmplitudeEnvelope?.Average();


    internal double TimeStep { get; set; }
    internal double FrequencyStep { get; set; }
    public List<Complex[]> Spectrogram { get; internal set; }
    public List<double> BandEnergyRatios { get; internal set; }
    // Vocals typically have < 100 Average BER while electronic music have > 120
    public double? AverageBandEnergyRatio => BandEnergyRatios?.Average();

    public List<double> SpectralCentroids { get; internal set; }
    public double? AverageSpectralCentroid => SpectralCentroids?.Average();

    // Psychoacoustic features
    //MFCCs reflect the nature of the sound
    public List<double[]> MFCC { get; internal set; }

    public Song(string filePath) => FilePath = filePath;


}