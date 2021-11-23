
using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

public class RootMeanSquareExtractor : IFeatureExtractor
{
    public int BlockLength { get; set; } = 2048;
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.RootMeanSquares = GetRootMeanSquares(reader);
        }
        return song;
    }

    private List<double> GetRootMeanSquares(IMusicFileStream reader)
    {
        var rootMeanSquares = new List<double>();
        var sampleBlock = reader.ReadBlock(BlockLength);
        while (sampleBlock.Count() == BlockLength)
        {
            rootMeanSquares.Add(CalculateRootMeanSquares(sampleBlock));
            sampleBlock = reader.ReadBlock(BlockLength);
        }
        return rootMeanSquares;
    }

    private double CalculateRootMeanSquares(List<float[]> sampleBlock)
    {
        var meanChannelDataSquared = sampleBlock.Select(channelSamples =>
             Math.Pow(channelSamples.Average(), 2));
        return Math.Sqrt(meanChannelDataSquared.Average());
    }
}