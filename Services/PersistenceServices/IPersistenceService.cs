namespace AudioAnalyzer.Services;

public interface IPersistenceService
{
    Task Save(List<Song> songs, string path);

    Task Append(List<Song> songs, string path);
    Task<List<Song>> Load(string path);
}
