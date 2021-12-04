namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// This class assigns the song label based on the parent directory name.
/// Songs should be organised into directories which reflect their label.
/// </summary>
public class DirectoryLabelExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        song.Label = (Directory.GetParent(song.FilePath)?.Name) ?? String.Empty;
        return song;
    }
}