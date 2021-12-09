using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Controller class to orchestrate loading of songs and processing of features.
/// </summary>
public class AudioAnalyzerController
{
    public readonly FeatureExtractionPipeline FeatureExtractionPipeline;
    private readonly IPersistenceService _persistenceService;

    private readonly ILogger<AudioAnalyzerController> _logger;
    public List<Song> Songs { get; init; } = new();
    public AudioAnalyzerController(FeatureExtractionPipeline featureExtractionPipeline,
                                   IPersistenceService persistenceService,
                                   ILogger<AudioAnalyzerController> logger)
    {
        FeatureExtractionPipeline = featureExtractionPipeline;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    /// <summary>
    /// This method loads the songs from a given path or directory.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>List of loaded songs</returns>
    public List<Song> LoadSongs(string path)
    {
        if (IsValidSongFile(path))
        {
            return InitialiseSongFromFile(path);
        }
        if (Directory.Exists(path))
        {
            return InitialiseSongsFromDirectory(path);
        }
        throw new ArgumentException("Invalid path supplied.");
    }

    private static bool IsValidSongFile(string path)
    {
        return Path.GetExtension(path) != null && File.Exists(path)
                    && MusicFileStreamFactory.SupportedFormats.Contains(Path.GetExtension(path));
    }

    public List<Song> LoadSongs(IEnumerable<Song> songs)
    {
        Songs.AddRange(songs);
        return Songs;
    }

    public void ClearSongs()
    {
        Songs.Clear();
    }

    private List<Song> InitialiseSongFromFile(string path)
    {
        Songs.Add(new Song { FilePath = path });
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

    /// <summary>
    /// This method processes all loaded songs through the supplied feature extraction pipeline
    /// At completion the song will be populated with its extracted features.
    /// </summary>
    public List<Song> ProcessFeatures()
    {
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        foreach (var song in Songs)
        {
            FeatureExtractionPipeline.Process(song);
            Console.SetCursorPosition(left, top);
            Console.Write($"Completed {Songs.IndexOf(song) + 1} songs out of {Songs.Count}");
        }
        _logger.LogInformation($"Processing of {Songs.Count} songs completed");
        return Songs;
    }

    /// <summary>
    /// Processes all songs asynchronously through the pipeline with each feature also being executed
    /// asynchronously
    /// </summary>
    /// <returns></returns>
    public async Task<List<Song>> ProcessFeaturesAsync()
    {
        var watch = Stopwatch.StartNew();
        var taskList = new List<Task>();
        foreach (var song in Songs)
        {
            taskList.Add(Task.Run(async () =>
              {
                  await FeatureExtractionPipeline.ProcessAsync(song);
              }));
        }
        await Task.WhenAll(taskList);
        _logger.LogInformation($"Processing completed in {watch.ElapsedMilliseconds} ms");
        return Songs;
    }

    /// <summary>
    /// Saves the extracted features to disk
    /// </summary>
    /// <param name="path">File path to save</param>
    /// <returns></returns>
    public async Task SaveFeatures(string path)
    {
        await _persistenceService.Save(Songs, path);
    }

    /// <summary>
    /// Appends the extracted features to a file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task AppendFeatures(string path)
    {
        await _persistenceService.Append(Songs, path);
    }

    /// <summary>
    /// Loads the extracted features into songs
    /// </summary>
    /// <param name="path">File path to load</param>
    /// <returns></returns>
    public async Task InitialiseFeatures(string path)
    {
        var songs = await _persistenceService.Load(path);
        Songs.AddRange(songs);
    }

}