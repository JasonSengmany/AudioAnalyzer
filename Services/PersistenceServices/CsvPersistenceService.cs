
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

            csv.WriteHeader<Song>();
            for (var i = 1; i <= songs.First().MFCC.Length; i++)
            {
                csv.WriteField($"MFCC Coeff {i}");
            }
            await csv.NextRecordAsync();
            foreach (var song in songs)
            {
                csv.WriteRecord(song);
                foreach (var mfcc in song.MFCC)
                {
                    csv.WriteField(mfcc);
                }
                await csv.NextRecordAsync();
            }
        }
    }
    // public sealed class SongMap : ClassMap<Song>
    // {
    //     public SongMap()
    //     {
    //         AutoMap(CultureInfo.InvariantCulture);
    //         Map(song => song.MFCC).Index(typeof(Song).GetProperties().Length);
    //     }
    // }
}