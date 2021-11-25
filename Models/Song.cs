namespace AudioAnalyser.Models;

public record Song
{
    public string FilePath { get; init; } = String.Empty;

    //Temporal Features
    public TimeSpan TotalTime { get; set; }
    public int BeatsPerMinute { get; set; }
    public List<double> ZeroCrossingRates { get; set; }
    public double? AverageZeroCrossingRate => ZeroCrossingRates?.Average();
    public List<double> RootMeanSquares { get; set; }
    public double? AverageRootMeanSquare => RootMeanSquares?.Average();
    public List<float> AmplitudeEnvelope { get; set; }
    public float? AverageEnvelope => AmplitudeEnvelope?.Average();


    //Frequency domain features
    public List<double> BandEnergyRatio { get; set; }
    public double? AverageBandEnergyRatio => BandEnergyRatio?.Average();
    // Psychoacoustic features
    public List<double[]> MFCC { get; set; }
    public Song(string filePath) => FilePath = filePath;


}