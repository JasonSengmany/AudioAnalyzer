
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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
    // public sealed class SongMap : ClassMap<Song>
    // {
    //     public SongMap()
    //     {
    //         AutoMap(CultureInfo.InvariantCulture);
    //         Map(song => song.BeatsPerMinute).Optional();
    //         Map(song => song.AverageBandEnergyRatio).Optional();
    //         Map(song => song.AverageBandwidth).Validate(field => field.Field != null);
    //     }
    // }
}