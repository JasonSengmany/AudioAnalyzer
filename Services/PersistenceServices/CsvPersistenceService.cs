
using System.Globalization;
using AudioAnalyzer.Services;
using CsvHelper;
using CsvHelper.Configuration;

public class CsvPersistenceService : IPersistenceService
{
    public async Task<List<Song>> Load(string path)
    {
        List<Song> songs = new();

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            songs = csv.GetRecords<Song>().ToList();
        }
        return songs;
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