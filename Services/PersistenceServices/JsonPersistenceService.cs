using AudioAnalyzer.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
public class JsonPersistenceService : IPersistenceService
{
    public async Task<List<Song>> Load(string path)
    {
        if (!File.Exists(path) && Path.GetExtension(path) != ".json")
        {
            return new();
        }
        using FileStream openStream = File.OpenRead(path);
        JsonSerializerOptions options = new()
        {
            IncludeFields = true
        };
        var songs = await JsonSerializer.DeserializeAsync<List<Song>>(openStream, options);
        await openStream.DisposeAsync();
        return songs ?? new();
    }

    public async Task Save(List<Song> songs, string path)
    {
        using FileStream createStream = File.Create(path);
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        await JsonSerializer.SerializeAsync(createStream, songs, options);
        await createStream.DisposeAsync();
    }
}