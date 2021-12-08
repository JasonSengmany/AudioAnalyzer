namespace AudioAnalyzer.FeatureExtraction;


/// <summary>
/// This abstract class implements the composite pattern and provides control structures that 
/// allow pre and post actions to be executed before child extractors are used. This ensures 
/// temporary properties that child extractors require are only cleared once all child extractions 
/// have completed.
/// </summary>
public abstract class PrerequisiteExtractor : IFeatureExtractor
{
    public List<IFeatureExtractor> DependentExtractors { get; private set; } = new List<IFeatureExtractor>();

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

    /// <summary>
    /// Method to add dependent extractors to the composite
    /// </summary>
    /// <param name="childExtractor"></param>
    public void AddChild(IFeatureExtractor childExtractor)
    {
        DependentExtractors.Add(childExtractor);
    }

    /// <summary>
    /// Method to remove dependent extractors from the composite
    /// </summary>
    /// <param name="childExtractor"></param>
    public void RemoveChild(IFeatureExtractor childExtractor)
    {
        DependentExtractors.Remove(childExtractor);
    }


}