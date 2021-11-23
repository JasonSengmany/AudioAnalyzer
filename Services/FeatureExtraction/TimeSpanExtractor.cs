using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

public class TimeSpanExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.TotalTime = reader.TotalTime;
        }
        return song;
    }
}