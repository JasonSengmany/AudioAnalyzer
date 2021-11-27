using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

/// <summary>
/// Used to perform a short time fourier transform of the signal. 
/// Prerequisite to other extractors such as <c>BandEnergyRatioExtractor</c>,
/// <c>SpectralCentroidExtractor</c> and <c>MfccExtractor</c>
/// </summary>
public class FrequecySpectrogramExtractor : IFeatureExtractor
{
    public int FrameSize { get; set; }
    public int HopLength { get; set; }
    public WindowFunction window { get; set; } = new HammingWindow();

    public FrequecySpectrogramExtractor(int frameSize = 2048, int hopLength = 512)
        => (FrameSize, HopLength) = (frameSize, hopLength);

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
            window.ApplyWindow(frame);
            var frequencySpectrum = FourierTransform.Radix2FFT(frame.ToArray());
            spectrogram.Add(frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray());
        }
        return spectrogram;
    }
}