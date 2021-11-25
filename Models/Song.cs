namespace AudioAnalyser.Models;

public record Song
{
    public string FilePath { get; init; } = String.Empty;
    public TimeSpan TotalTime { get; set; }
    public int BeatsPerMinute { get; set; }
    public List<double> ZeroCrossingRates { get; set; }
    public double AverageZeroCrossingRate => ZeroCrossingRates?.Average() ?? 0;
    public List<double> RootMeanSquares { get; set; }
    public double AverageRootMeanSquare => RootMeanSquares?.Average() ?? 0;
    public List<float> AmplitudeEnvelope { get; set; }
    public float AverageEnvelope => AmplitudeEnvelope?.Average() ?? 0;
    public List<double[]> MFCC { get; set; }
    public Song(string filePath) => FilePath = filePath;


}