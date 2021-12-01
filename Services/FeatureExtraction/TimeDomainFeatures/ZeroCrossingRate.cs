using AudioAnalyzer.MusicFileReader;

namespace AudioAnalyzer.FeatureExtraction;

public class ZeroCrossingRateExtractor : IFeatureExtractor
{
    public int BlockLength { get; set; } = 2048;

    public List<double> GetZeroCrossingRates(IMusicFileStream reader)
    {

        var zeroCrossingRates = new List<double>();
        var sampleBlock = reader.ReadBlock(BlockLength);
        while (sampleBlock.Count() == BlockLength)
        {
            zeroCrossingRates.Add(CalculateZeroCrossingRate(sampleBlock));
            sampleBlock = reader.ReadBlock(BlockLength);
        }
        return zeroCrossingRates;
    }

    private double CalculateZeroCrossingRate(List<float[]> blockData)
    {
        var meanChannelDataSign = blockData.Select(x => x.Average())
                .Select(sample => (sample > 0) ? 1 : (sample < 0) ? -1 : 0).ToList();
        return 0.5f * meanChannelDataSign
            .Zip(meanChannelDataSign.Skip(1), (x, y) => y - x)
            .Select(x => Math.Abs(x))
            .Average();
    }

    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.ZeroCrossingRates = GetZeroCrossingRates(reader);
        }
        return song;
    }
}