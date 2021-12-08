
using System.Globalization;
using CsvHelper;

namespace AudioAnalyzer.Services;

public class CsvPersistenceService : IPersistenceService
{
    public async Task Append(List<Song> songs, string path)
    {
        if (!File.Exists(path) || Path.GetExtension(path) != ".csv")
        {
            throw new ArgumentException("Invalid path provided");
        }

        using (var stream = File.Open(path, FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
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
}