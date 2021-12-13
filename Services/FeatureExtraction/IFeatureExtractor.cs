namespace AudioAnalyzer.FeatureExtraction;

public interface IFeatureExtractor
{
    Song ExtractFeature(Song song);

    List<string> GetCompleteFeatureExtractorNames()
    {
        return new() { this.GetType().Name };
    }

    List<IFeatureExtractor> GetAllFeatureExtractors()
    {
        return new() { this };
    }
}
