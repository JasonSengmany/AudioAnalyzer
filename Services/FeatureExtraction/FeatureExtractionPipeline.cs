namespace AudioAnalyzer.FeatureExtraction;

public class FeatureExtractionPipeline
{
    public List<IFeatureExtractor> _featurizers { get; private set; } = new() { };

    public FeatureExtractionPipeline() { }
    public FeatureExtractionPipeline(params IFeatureExtractor[] featurizers)
    {
        Load(featurizers);
    }
    public void Load(params IFeatureExtractor[] featurizers)
    {
        foreach (var featurizer in featurizers)
        {
            var attr = Attribute.GetCustomAttribute(featurizer.GetType(), typeof(PrerequisiteExtractorAttribute));
            if (attr is null)
            {
                _featurizers.Add(featurizer);
            }
            else
            {
                LoadFeaturizerWithPrerequisite(featurizer, ((PrerequisiteExtractorAttribute)attr).GetPrerequisiteStack());
            }
        }
    }
    private void LoadFeaturizerWithPrerequisite(IFeatureExtractor featurizer, Stack<string> prerequisite)
    {
        var parentPrerequisite = prerequisite.Pop();
        var parentPrerequisiteType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{parentPrerequisite}");
        if (parentPrerequisiteType is null)
        {
            throw new FeaturePipelineException("Unable to resolve prequisite type");
        }
        var parentFeaturizer = _featurizers.Where((featurizer) =>
        {
            return featurizer.GetType().IsAssignableTo(parentPrerequisiteType);
        }).FirstOrDefault();
        if (parentFeaturizer is null)
        {
            if (!parentPrerequisiteType.IsAbstract)
            {
                var initialisedPrerequisite = Activator.CreateInstance(parentPrerequisiteType);
                if (initialisedPrerequisite is null)
                {
                    return;
                }
                ((PrerequisiteExtractor)initialisedPrerequisite).AddChild(featurizer, prerequisite);
                _featurizers.Add((PrerequisiteExtractor)initialisedPrerequisite);
            }
        }
        else
        {
            ((PrerequisiteExtractor)parentFeaturizer).AddChild(featurizer, prerequisite);
        }
    }
    public Song Process(Song song)
    {
        foreach (var featurizer in _featurizers)
        {
            featurizer.ExtractFeature(song);
        }
        return song;
    }

    public async Task<Song> ProcessAsync(Song song)
    {
        var taskList = new List<Task<Song>>();
        foreach (var featurizer in _featurizers)
        {
            taskList.Add(Task.Run(() => featurizer.ExtractFeature(song)));
        }
        return (await Task.WhenAll<Song>(taskList)).First();
    }

}
