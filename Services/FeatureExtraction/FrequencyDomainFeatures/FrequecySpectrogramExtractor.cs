using System.Numerics;
using AudioAnalyzer.DSPUtils;
using AudioAnalyzer.Services;
using MathNet.Numerics.Providers.FourierTransform;

namespace AudioAnalyzer.FeatureExtraction;
/// <summary>
/// Used to perform a short time fourier transform of the signal. 
/// Prerequisite to other extractors such as <c>BandEnergyRatioExtractor</c>,
/// <c>SpectralCentroidExtractor</c> and <c>MfccExtractor</c>. The spectrogram is
/// cleared after all child extractors have executed.
/// </summary>
public class FrequecySpectrogramExtractor : PrerequisiteExtractor
{
    public int FrameSize { get; set; } = 2048;
    public int HopLength { get; set; } = 512;
    public WindowFunction window { get; set; } = new HammingWindow();
    public FrequecySpectrogramExtractor(params IFeatureExtractor[] dependentExtractors)
    : base(dependentExtractors) { }

    protected override void PreFeatureExtraction(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song._metadata.Add("Spectrogram", GetSpectrogram(reader));
            song._metadata.Add("TimeStep", (double)HopLength / reader.SampleRate);
            song._metadata.Add("FrequencyStep", (double)reader.SampleRate / FrameSize);
        }
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

    protected override void PostFeatureExtraction(Song song)
    {
        song._metadata.Remove("Spectrogram");
        song._metadata.Remove("FrequencyStep");
        song._metadata.Remove("TimeStep");
    }
}