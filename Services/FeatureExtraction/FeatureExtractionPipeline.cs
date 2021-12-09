using System.Reflection;

namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// This class contains a list of feature extractors that are used to process Song objects.
/// </summary>
public class FeatureExtractionPipeline
{
    public List<IFeatureExtractor> _featurizers { get; private set; } = new() { };
    public FeatureExtractionPipeline() { }
    public FeatureExtractionPipeline(params IFeatureExtractor[] featurizers)
    {
        foreach (var featurizer in featurizers)
        {
            Load(featurizer);
        }
    }

    /// <summary>
    /// Loads a new feature extractor based on the class name.
    /// </summary>
    /// <param name="featurizerName">Name of the featurizer class</param>
    /// <returns>true if successfully loaded and false otherwise</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    public void Load(string featurizerName)
    {
        var featurizerType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{featurizerName}");
        if (featurizerType is null) throw new ArgumentException($"Unable to recognise feature extractor {featurizerName}");
        var featurizerInstance = Activator.CreateInstance(featurizerType, BindingFlags.CreateInstance
                                                                          | BindingFlags.Public
                                                                          | BindingFlags.Instance
                                                                          | BindingFlags.OptionalParamBinding,
                                                                            null, new Object[] { }, null);
        if (featurizerInstance is null) throw new NullReferenceException("Unable to initialise feature extracor");
        Load((IFeatureExtractor)featurizerInstance);
    }

    /// <summary>
    /// Initialises an instance of the featurizerName and loads it into the feature extraction pipeline.
    /// </summary>
    /// <param name="featurizerName">name of an <c>IFeatureExtractor</c></param>
    /// <returns><c>true</c> if featurizer was succesfully loaded; otherwise <c>false</c> </returns>
    public bool TryLoad(string featurizerName)
    {
        try
        {
            Load(featurizerName);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    /// <summary>
    /// Loads a feature extractors
    /// </summary>
    /// <param name="featurizers"></param>
    /// <returns>true on success otherwise false if an extractor had failed to be loaded</returns>
    public void Load(IFeatureExtractor featurizer)
    {
        var prerequisites = PrerequisiteExtractorAttribute.GetPrerequisites(featurizer);
        if (prerequisites.Count != 0)
        {
            LoadFeaturizerWithPrerequisite(featurizer, prerequisites);
        }
        _featurizers.Add(featurizer);
    }

    public bool TryLoad(IFeatureExtractor featurizer)
    {
        try
        {
            Load(featurizer);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    /// <summary>
    /// Loads a feature extractor with prerequisite extractors.
    /// </summary>
    /// <param name="featurizer"></param>
    /// <param name="prerequisite">Stack of prerequisite classes with the top of the stack being 
    /// the root requirement</param>
    /// <returns>true on success and false if an extractor had failed to be loaded</returns>
    /// <exception cref="FeaturePipelineException"></exception>
    private void LoadFeaturizerWithPrerequisite(IFeatureExtractor featurizer, Stack<string> prerequisites)
    {
        var rootPrerequisite = prerequisites.Pop();
        var rootPrerequisiteType = Type.GetType($"AudioAnalyzer.FeatureExtraction.{rootPrerequisite}");
        if (rootPrerequisiteType is null)
        {
            throw new FeaturePipelineException($"Unable to resolve prequisite type {rootPrerequisite}");
        }
        var parentFeaturizer = _featurizers.Where((featurizer) =>
        {
            return featurizer.GetType().IsAssignableTo(rootPrerequisiteType);
        }).FirstOrDefault();
        if (parentFeaturizer is null)
        {
            InitializeRootPrerequisiteAndLoad(featurizer, prerequisites, rootPrerequisiteType);
        }
        else
        {
            ((PrerequisiteExtractor)parentFeaturizer).AddChild(featurizer, prerequisites);
        }
    }

    private void InitializeRootPrerequisiteAndLoad(IFeatureExtractor featurizer, Stack<string> prerequisites, Type rootPrerequisiteType)
    {
        if (!rootPrerequisiteType.IsAbstract)
        {
            var initialisedPrerequisite = Activator.CreateInstance(rootPrerequisiteType);
            if (initialisedPrerequisite is null) throw new FeaturePipelineException("Unable to initialize prerequisite");
            ((PrerequisiteExtractor)initialisedPrerequisite).AddChild(featurizer, prerequisites);
            _featurizers.Add((PrerequisiteExtractor)initialisedPrerequisite);
        }
        throw new FeaturePipelineException("Unable to initialize abstract extractor class");
    }

    /// <summary>
    /// This methid processes a Song object and sets all properties related to the loaded extractors
    /// </summary>
    /// <param name="song"></param>
    /// <returns>Reference to passed in Song</returns>
    public Song Process(Song song)
    {
        if (_featurizers.Count == 0) return song;
        foreach (var featurizer in _featurizers)
        {
            featurizer.ExtractFeature(song);
        }
        return song;
    }

    public async Task<Song> ProcessAsync(Song song)
    {
        if (_featurizers.Count == 0) return song;
        var taskList = new List<Task<Song>>();
        foreach (var featurizer in _featurizers)
        {
            taskList.Add(Task.Run(() => featurizer.ExtractFeature(song)));
        }
        return (await Task.WhenAll<Song>(taskList)).First();
    }

}
