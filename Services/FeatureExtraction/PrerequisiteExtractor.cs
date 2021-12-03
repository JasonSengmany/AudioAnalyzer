namespace AudioAnalyzer.FeatureExtraction;


/// <summary>
/// This abstract class provides control structures that allow pre and post actions to be executed
/// before child extractors are used. This ensures temporary properties that child extractors require
/// are only cleared once all child extractions have completed.
/// </summary>
public abstract class PrerequisiteExtractor : IFeatureExtractor
{
    public List<IFeatureExtractor> DependentExtractors = new List<IFeatureExtractor>();

    public PrerequisiteExtractor(params IFeatureExtractor[] dependentExtractors)
    {
        DependentExtractors.AddRange(dependentExtractors);
    }

    /// <summary>
    /// Template method that performs the <c>PreFeatureExtraction</c> and <c>PostFeatureExtraction</c>
    /// around all dependent children.
    /// </summary>
    /// <param name="song"></param>
    /// <returns>Song with extracted features</returns>
    public Song ExtractFeature(Song song)
    {
        PreFeatureExtraction(song);
        foreach (var extractor in DependentExtractors)
        {
            extractor.ExtractFeature(song);
        }
        PostFeatureExtraction(song);
        return song;
    }

    /// <summary>
    /// Method to be performed before executing dependent extractors
    /// </summary>
    /// <param name="song"></param>
    protected abstract void PreFeatureExtraction(Song song);

    /// <summary>
    /// Method to be performed after executing dependent extractors
    /// </summary>
    /// <param name="song"></param>
    protected abstract void PostFeatureExtraction(Song song);

}