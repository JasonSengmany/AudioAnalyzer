using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;

public abstract class BeatDetector : IFeatureExtractor
{
    protected int _instantBufferLength = 1024;
    protected int _historyBufferLength = 43;

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