using System.Numerics;
using AudioAnalyzer.DSPUtils;
using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;
public class HalfHannEnvelopeDetector : EnvelopeDetector
{
    protected override List<float> GetAmplitudeEnvelope(IMusicFileStream reader)
    {
        var musicData = reader.ReadAll().Select(x => new Complex(x[0], x[1])).ToArray();
        var halfHannWindow = new HannWindow();
        var windowCoeff = halfHannWindow.GetWindowCoefficients((int)(2 * reader.SampleRate * 0.4))
                              .Skip((int)(reader.SampleRate * 0.4))
                              .ToArray();
        var window = new double[musicData.Length];
        windowCoeff.CopyTo(window, 0);
        var windowFrequencySpectrum = FourierTransform.Radix2FFT(window);

        var frequencySpectrum = FourierTransform.Radix2FFT(musicData);
        var lowPassFilteredMusicData = frequencySpectrum.Zip(windowFrequencySpectrum, (val, coeff) => val * coeff).ToArray();
        var envelope = FourierTransform.Radix2IFFT(lowPassFilteredMusicData);

        var plt = new ScottPlot.Plot();
        plt.AddSignal(musicData.Select(x => x.Magnitude).ToArray());
        plt.AddSignal(envelope.Select(x => x.Magnitude).ToArray());

        plt.SaveFig("envelop.png");
        return envelope.Select(x => (float)x.Magnitude).ToList();
    }
}