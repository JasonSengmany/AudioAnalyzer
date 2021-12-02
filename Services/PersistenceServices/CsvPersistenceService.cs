
using System.Globalization;
using CsvHelper;

namespace AudioAnalyzer.Services;

public class CsvPersistenceService : IPersistenceService
{
    public Task<List<Song>> Load(string path)
    {
        List<Song> songs;

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            songs = csv.GetRecords<Song>().ToList();
        }
        return Task.FromResult(songs);
    }

    public async Task Save(List<Song> songs, string path)
    {
        using (var writer = new StreamWriter(path))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(songs);
        }
    }
}