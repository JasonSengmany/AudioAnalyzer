using AudioAnalyzer.Models;

namespace AudioAnalyzer.FeatureExtraction;

public class FeatureExtractionPipeline
{
    public List<IFeatureExtractor> _featurizers { get; private set; } = new() { new TimeSpanExtractor() };

    public FeatureExtractionPipeline() { }
    public FeatureExtractionPipeline(params IFeatureExtractor[] featurizers)
    {
        Load(featurizers);
    }
    public void Load(params IFeatureExtractor[] featurizers)
    {
        _featurizers.AddRange(featurizers);
    }

    public Song Process(Song song)
    {
        foreach (var featurizer in _featurizers)
        {
            featurizer.ExtractFeature(song);
        }
        return song;
    }
}

public interface IFeatureExtractor
{
    Song ExtractFeature(Song song);
}
