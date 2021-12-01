using AudioAnalyzer.Models;
using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.MusicFileReader;
using AudioAnalyzer.Services;

public class AudioAnalyzerController
{
    public FeatureExtractionPipeline FeatureExtractionPipeline;

    private IPersistenceService _persistenceService;
    public List<Song> Songs { get; init; } = new();
    public AudioAnalyzerController(FeatureExtractionPipeline featureExtractionPipeline, IPersistenceService persistenceService)
    {
        FeatureExtractionPipeline = featureExtractionPipeline;
        _persistenceService = persistenceService;
    }
    public List<Song> LoadSongs(string path)
    {
        if (Path.GetExtension(path) != null && MusicFileStreamFactory.SupportedFormats.Contains(Path.GetExtension(path)))
        {
            Songs.Add(new Song(path));
            return Songs;
        }
        var musicFilesInFolder = Directory.EnumerateFileSystemEntries(path, "*.*", searchOption: SearchOption.AllDirectories)
            .Where(filename => MusicFileStreamFactory.SupportedFormats.Contains(Path.GetExtension(filename)));
        foreach (var musicFilePath in musicFilesInFolder)
        {
            Songs.Add(new Song(musicFilePath));
        }
        return Songs;
    }

    public void ProcessFeatures()
    {
        foreach (var song in Songs)
        {
            FeatureExtractionPipeline.Process(song);
        }
    }

    public async Task SaveFeatures(string path)
    {
        await _persistenceService.Save(Songs, path);
    }

    public async Task LoadFeatures(string path)
    {
        var songs = await _persistenceService.Load(path);
        Songs.AddRange(songs);
    }

}