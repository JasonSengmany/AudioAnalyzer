
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

public static class MusicFileStreamFactory
{
    public static IMusicFileStream GetStreamReader(Song song) => Path.GetExtension(song.FilePath).ToLower() switch
    {
        ".wav" => new WaveFileStream(song.FilePath),
        ".flac" => new FlacFileStream(song.FilePath),
        ".mp3" => new MP3FileStream(song.FilePath),
        _ => throw new ArgumentException($"Unsupported file type {Path.GetExtension(song.FilePath)}"),
    };
}