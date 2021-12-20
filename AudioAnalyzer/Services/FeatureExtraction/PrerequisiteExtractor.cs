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
    internal void AddChild(IFeatureExtractor childExtractor, Stack<string> prerequisites)
    {
        if (ContainsType(childExtractor.GetType())) throw new FeaturePipelineException("Unable to load duplicate typed extractor");
        if (prerequisites.Count == 0)
        {
            AddChild(childExtractor);
            return;
        }
        var parentPrerequisite = prerequisites.Pop();
        var parentPrerequisiteType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{parentPrerequisite}");
        if (parentPrerequisiteType is null) throw new FeaturePipelineException("Unable to resolve prerequisite type");
        if (this.GetType().IsAssignableTo(parentPrerequisiteType))
        {
            AddChild(childExtractor);
            return;
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
                if (initialisedPrerequisite is null) throw new FeaturePipelineException("Unable to initialize prerequisite");
                ((PrerequisiteExtractor)initialisedPrerequisite).AddChild(childExtractor);
                DependentExtractors.Add(((PrerequisiteExtractor)initialisedPrerequisite));
                return;
            }
            throw new FeaturePipelineException("Unable to initialize abstract extractor class");
        }
        else
        {
            ((PrerequisiteExtractor)parentFeaturizer).AddChild(childExtractor, prerequisites);
        }
    }

    private bool ContainsType(Type childExtractorType)
    {
        return DependentExtractors.Where(extractor => extractor.GetType().IsAssignableTo(childExtractorType)).Any();
    }

    /// <summary>
    /// Method to remove dependent extractors from the composite
    /// </summary>
    /// <param name="childExtractor"></param>
    public void RemoveChild(IFeatureExtractor childExtractor)
    {
        DependentExtractors.Remove(childExtractor);
    }

    public List<string> GetCompleteFeatureExtractorNames()
    {
        var names = new List<string>() { this.GetType().Name };
        foreach (var extractor in DependentExtractors)
        {
            names.AddRange(extractor.GetCompleteFeatureExtractorNames());
        }
        return names;
    }

    public List<IFeatureExtractor> GetAllFeatureExtractors()
    {
        var extractors = new List<IFeatureExtractor>();
        extractors.Add(this);
        foreach (var extractor in DependentExtractors)
        {
            extractors.AddRange(extractor.GetAllFeatureExtractors());
        }
        return extractors;
    }

    internal bool RemoveChild(IFeatureExtractor featurizer, Stack<string> prerequisites)
    {
        if (ContainsType(featurizer.GetType()))
        {
            return DependentExtractors.Remove(featurizer);
        }
        if (!prerequisites.Any()) return false;
        var nextPrerequisite = prerequisites.Pop();
        var nextPrerequisiteType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{nextPrerequisite}");
        if (nextPrerequisiteType is null) throw new FeaturePipelineException("Unable to resolve prerequisite type");
        var nextFeaturizer = DependentExtractors.Where((extractor) =>
        {
            return extractor.GetType().IsAssignableTo(nextPrerequisiteType);
        }).FirstOrDefault();

        if (nextFeaturizer is null)
        {
            return false;
        }

        return ((PrerequisiteExtractor)nextFeaturizer).RemoveChild(featurizer, prerequisites);
    }
}