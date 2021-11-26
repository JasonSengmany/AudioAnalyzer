using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

public class FrequecySpectrogramExtractor : IFeatureExtractor
{
    public int FrameSize { get; set; } = 2048;
    public int HopLength { get; set; } = 512;
    public WindowFunction window { get; set; } = new HammingWindow();
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.Spectrogram = GetSpectrogram(reader);
            song.TimeStep = (double)HopLength / reader.SampleRate;
            song.FrequencyStep = (double)reader.SampleRate / FrameSize;
        }
        return song;
    }

    private List<Complex[]> GetSpectrogram(IMusicFileStream reader)
    {

        var musicData = reader.ReadAll()
            .Select(channelData => new Complex(channelData[0], channelData[1])).ToList();
        var sampleFrames = FourierTransform.PartitionToFrames(musicData, FrameSize, HopLength);
        var spectrogram = new List<Complex[]>(sampleFrames.Count());
        foreach (var frame in sampleFrames)
        {
            var windowedFrame = window.ApplyWindow(frame);
            var frequencySpectrum = FourierTransform.Radix2FFT(windowedFrame.ToArray());
            frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray();
            spectrogram.Add(frequencySpectrum);
        }
        return spectrogram;
    }
}