using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.Models;
using AudioAnalyzer.MusicFileReader;

public class TimeSpanExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.TotalTime = reader.TotalTime.TotalSeconds;
        }
        return song;
    }
}