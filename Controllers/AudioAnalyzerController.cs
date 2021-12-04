using AudioAnalyzer.FeatureExtraction;
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
            return InitialiseSongFromFile(path);
        }
        else
        {
            return InitialiseSongsFromDirectory(path);
        }

    }

    private List<Song> InitialiseSongFromFile(string path)
    {
        Songs.Add(new Song()
        {
            FilePath = path
        });
        return Songs;
    }

    private List<Song> InitialiseSongsFromDirectory(string path)
    {
        var musicFilesInFolder = Directory.EnumerateFileSystemEntries(path, "*.*", searchOption: SearchOption.AllDirectories)
                                .Where(filename => MusicFileStreamFactory.SupportedFormats.Contains(Path.GetExtension(filename)));
        foreach (var musicFilePath in musicFilesInFolder)
        {
            InitialiseSongFromFile(musicFilePath);
        }
        return Songs;
    }

    public void ProcessFeatures()
    {
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        foreach (var song in Songs)
        {
            FeatureExtractionPipeline.Process(song);
            Console.SetCursorPosition(left, top);
            Console.Write($"Completed {Songs.IndexOf(song) + 1} songs out of {Songs.Count}");

        }
        Console.WriteLine("\nProcessing complete!");
    }

    public async Task ProcessFeaturesAsync()
    {
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        var taskList = new List<Task>();
        var completedSongs = 0;
        foreach (var song in Songs)
        {
            taskList.Add(Task.Run(async () =>
              {
                  await FeatureExtractionPipeline.ProcessAsync(song);
                  lock (this)
                  {
                      Console.SetCursorPosition(left, top);
                      Console.Write($"Completed {++completedSongs} songs out of {Songs.Count}");
                  }
              }));
        }
        await Task.WhenAll(taskList);
        Console.WriteLine("\nProcessing complete!");
    }
    public async Task SaveFeatures(string path)
    {
        await _persistenceService.Save(Songs, path);
    }

    public async Task InitialiseFeatures(string path)
    {
        var songs = await _persistenceService.Load(path);
        Songs.AddRange(songs);
    }

}