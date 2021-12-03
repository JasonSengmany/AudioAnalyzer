namespace AudioAnalyzer.FeatureExtraction;

public interface IFeatureExtractor
{
    Song ExtractFeature(Song song);
}
