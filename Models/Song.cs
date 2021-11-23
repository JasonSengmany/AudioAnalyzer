namespace AudioAnalyser.Models;

public record Song
{
    public string FilePath { get; init; } = String.Empty;
    public TimeSpan TotalTime { get; set; }
    public int BeatsPerMinute { get; set; }
    public List<double> ZeroCrossingRates { get; set; }
    public double AverageZeroCrossingRate { get => ZeroCrossingRates?.Average() ?? 0.0; }
    public List<double> RootMeanSquares { get; set; }
    public double AverageRootMeanSquare { get => RootMeanSquares?.Average() ?? 0.0; }
    public List<double[]> MFCC { get; set; }
    public Song(string filePath) => FilePath = filePath;


}