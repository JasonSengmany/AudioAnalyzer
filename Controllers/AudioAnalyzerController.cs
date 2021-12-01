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
            Songs.Add(new Song() { FilePath = path });
            return Songs;
        }
        var musicFilesInFolder = Directory.EnumerateFileSystemEntries(path, "*.*", searchOption: SearchOption.AllDirectories)
            .Where(filename => MusicFileStreamFactory.SupportedFormats.Contains(Path.GetExtension(filename)));
        foreach (var musicFilePath in musicFilesInFolder)
        {
            Songs.Add(new Song() { FilePath = musicFilePath });
        }
        return Songs;
    }

    public void ProcessFeatures()
    {
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        foreach (var song in Songs)
        {
            Console.SetCursorPosition(left, top);
            Console.Write($"Processing song {Songs.IndexOf(song) + 1} out of {Songs.Count}");
            FeatureExtractionPipeline.Process(song);
        }
        Console.WriteLine("\nProcessing complete!");
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