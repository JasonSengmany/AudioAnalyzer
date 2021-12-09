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
    /// Internal method to allow loading of extractors consisting of prerequisite chains.
    /// </summary>
    /// <param name="childExtractor"></param>
    /// <param name="prerequisites"></param>
    /// <returns>true on success otherwise false if an extractor had failed to be loaded</returns>
    internal bool AddChild(IFeatureExtractor childExtractor, Stack<string> prerequisites)
    {
        if (prerequisites.Count == 0)
        {
            AddChild(childExtractor);
            return true;
        }
        var parentPrerequisite = prerequisites.Pop();
        var parentPrerequisiteType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{parentPrerequisite}");
        if (parentPrerequisiteType is null) return false;
        if (this.GetType().IsAssignableTo(parentPrerequisiteType))
        {
            AddChild(childExtractor);
            return true;
        }
        var parentFeaturizer = DependentExtractors.Where((featurizer) =>
        {
            return featurizer.GetType().IsAssignableTo(parentPrerequisiteType);
        }).FirstOrDefault();

        if (parentFeaturizer is null)
        {
            if (!parentPrerequisiteType.IsAbstract)
            {
                var initialisedPrerequisite = Activator.CreateInstance(parentPrerequisiteType);
                if (initialisedPrerequisite is null) return false;
                ((PrerequisiteExtractor)initialisedPrerequisite).AddChild(childExtractor);
                DependentExtractors.Add(((PrerequisiteExtractor)initialisedPrerequisite));
                return true;
            }
            return false;
        }
        else
        {
            return ((PrerequisiteExtractor)parentFeaturizer).AddChild(childExtractor, prerequisites);
        }
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