using System.Numerics;

namespace AudioAnalyser.Models;

public record Song
{
    public string FilePath { get; init; } = String.Empty;

    //Temporal Features
    public TimeSpan TotalTime { get; internal set; }
    public int BeatsPerMinute { get; internal set; }
    public List<double> ZeroCrossingRates { get; internal set; }

    // Songs that are noisy and have large fluctuations in pitch have higher ZCR
    public double? AverageZeroCrossingRate => ZeroCrossingRates?.Average();
    public List<double> RootMeanSquares { get; internal set; }
    public double? AverageRootMeanSquare => RootMeanSquares?.Average();
    public List<float> AmplitudeEnvelope { get; internal set; }
    public float? AverageEnvelope => AmplitudeEnvelope?.Average();


    //Frequency domain features

    // Frequency spectrogram consisting of (timeStep, frequencyStep, spectrums for each frame)
    internal double TimeStep { get; set; }
    internal double FrequencyStep { get; set; }
    public List<Complex[]> Spectrogram { get; internal set; }
    public List<double> BandEnergyRatio { get; internal set; }
    // Vocals typically have < 100 Average BER while electronic music have > 120
    public double? AverageBandEnergyRatio => BandEnergyRatio?.Average();

    public List<double> SpectralCentroids { get; internal set; }
    public double? AverageSpectralCentroid => SpectralCentroids?.Average();

    // Psychoacoustic features
    //MFCCs reflect the nature of the sound
    public List<double[]> MFCC { get; internal set; }

    public Song(string filePath) => FilePath = filePath;


}