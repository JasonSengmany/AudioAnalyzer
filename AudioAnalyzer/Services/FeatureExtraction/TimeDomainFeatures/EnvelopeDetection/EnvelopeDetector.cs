using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;
public abstract class EnvelopeDetector : IFeatureExtractor
{
    protected abstract List<float> GetAmplitudeEnvelope(IMusicFileStream reader);
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.AverageEnvelope = GetAmplitudeEnvelope(reader).Average();
        }
        return song;
    }
}