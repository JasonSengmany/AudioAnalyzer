namespace AudioAnalyser.Models;

public record Song
{
    public string FilePath { get; init; }
    public TimeSpan TotalTime { get; set; }
    public int BeatsPerMinute { get; set; }
    public List<double> ZeroCrossingRates { get; set; }
    public double AverageZeroCrossingRate { get => ZeroCrossingRates.Average(); }
    public List<double> RootMeanSquares { get; set; }
    public double AverageRootMeanSquare { get => RootMeanSquares.Average(); }
    public List<double[]> MFCC { get; set; }
    public Song(string filePath) => FilePath = filePath;


}