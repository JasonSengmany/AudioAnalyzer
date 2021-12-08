using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;

public class ZeroCrossingRateExtractor : IFeatureExtractor
{
    public int BlockLength { get; set; } = 2048;

    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.AverageZeroCrossingRate = GetZeroCrossingRates(reader).Average();
        }
        return song;
    }
    public List<double> GetZeroCrossingRates(IMusicFileStream reader)
    {
        var sampleBlock = reader.ReadBlock(BlockLength);
        var zeroCrossingRates = new List<double>();
        while (sampleBlock.Count() == BlockLength)
        {
            zeroCrossingRates.Add(CalculateZeroCrossingRate(sampleBlock, reader.NumChannels));
            sampleBlock = reader.ReadBlock(BlockLength);
        }
        return zeroCrossingRates;
    }

    private double CalculateZeroCrossingRate(List<float[]> blockData, int numChannels)
    {
        var meanChannelDataSign = new int[BlockLength];
        for (var i = 0; i < blockData.Count - 1; i++)
        {
            var avgSample = (blockData[i].Sum() / numChannels);
            meanChannelDataSign[i] = (avgSample > 0) ? 1 : (avgSample < 0) ? -1 : 0;
        }
        var total = 0;
        for (var i = 0; i < meanChannelDataSign.Length - 1; i++)
        {
            var diff = meanChannelDataSign[i + 1] - meanChannelDataSign[i];
            total += diff < 0 ? -diff : diff;
        }
        return 0.5 * total / (meanChannelDataSign.Length - 1);
    }


}