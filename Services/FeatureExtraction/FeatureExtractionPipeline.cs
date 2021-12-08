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
