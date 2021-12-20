namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// This class allows custom song labelling to be performed based on a supplied function
/// </summary>
public class CustomLabelExtractor : IFeatureExtractor
{
    private Func<Song, string> _getLabelFunc;
    public CustomLabelExtractor(Func<Song, string> getLabelFunc)
    {
        _getLabelFunc = getLabelFunc;
    }
    public Song ExtractFeature(Song song)
    {
        song.Label = _getLabelFunc(song);
        return song;
    }
}