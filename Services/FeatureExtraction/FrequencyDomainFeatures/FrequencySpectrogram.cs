using System.Numerics;
using AudioAnalyzer.DSPUtils;
using AudioAnalyzer.Services;
using MathNet.Numerics.Providers.FourierTransform;

namespace AudioAnalyzer.FeatureExtraction;
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
            .Select(channelData => new Complex(channelData[0], channelData[1])).ToArray();
        var musicDataSpan = musicData.AsSpan();
        var spectrogram = new List<Complex[]>();
        for (var offset = 0; offset < musicData.Length - FrameSize; offset += HopLength)
        {
            var windowedFrame = window.ApplyWindow(musicDataSpan.Slice(offset, FrameSize)).ToArray();
            FourierTransformControl.Provider.Forward(windowedFrame, FourierTransformScaling.SymmetricScaling);
            spectrogram.Add(windowedFrame.Take(windowedFrame.Length / 2 + 1).ToArray());
        }
        return spectrogram;
    }
}