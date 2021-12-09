using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;
public class RootMeanSquareExtractor : IFeatureExtractor
{
    public int BlockLength { get; set; } = 2048;
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.AverageRootMeanSquare = GetRootMeanSquares(reader).Average();
        }
        return song;
    }

    private List<double> GetRootMeanSquares(IMusicFileStream reader)
    {
        var rootMeanSquares = new List<double>();
        var sampleBlock = reader.ReadBlock(BlockLength);
        while (sampleBlock.Count() == BlockLength)
        {
            rootMeanSquares.Add(CalculateRootMeanSquares(sampleBlock, reader.NumChannels));
            sampleBlock = reader.ReadBlock(BlockLength);
        }
        return rootMeanSquares;
    }

    private double CalculateRootMeanSquares(List<float[]> sampleBlock, int numChannels)
    {
        var meanChannelDataSquared = sampleBlock.Select(channelSamples =>
             Math.Pow(channelSamples.Sum() / numChannels, 2));
        return Math.Sqrt(meanChannelDataSquared.Average());
    }
}