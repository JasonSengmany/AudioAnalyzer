using AudioAnalyser.MusicFileReader;
using AudioAnalyser.Models;

namespace AudioAnalyser.FeatureExtraction;

public abstract class BeatDetector : IFeatureExtractor
{
    protected const int _instantBufferLength = 1024;
    protected const int _historyBufferLength = 43;

    public string BeatString { get; protected set; } = String.Empty;

    protected abstract int DetectBPM(IMusicFileStream reader);
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.BeatsPerMinute = DetectBPM(reader);
        }
        return song;
    }
}