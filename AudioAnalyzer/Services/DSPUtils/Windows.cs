using System.Numerics;

namespace AudioAnalyzer.DSPUtils;
public abstract class WindowFunction
{
    private double[]? _cachedWindowCoefficients = null;
    public List<double> ApplyWindow(List<double> samples)
    {
        if (_cachedWindowCoefficients == null || samples.Count != _cachedWindowCoefficients.Length)
        {
            _cachedWindowCoefficients = GetWindowCoefficients(samples.Count);
        }

        for (var i = 0; i < samples.Count; i++)
        {
            samples[i] *= _cachedWindowCoefficients[i];
        }
        return samples;
    }

    public List<Complex> ApplyWindow(List<Complex> samples)
    {
        if (_cachedWindowCoefficients == null || samples.Count != _cachedWindowCoefficients.Length)
        {
            _cachedWindowCoefficients = GetWindowCoefficients(samples.Count);
        }

        for (var i = 0; i < samples.Count; i++)
        {
            samples[i] *= _cachedWindowCoefficients[i];
        }
        return samples;
    }

    public List<Complex> ApplyWindow(ReadOnlySpan<Complex> samples)
    {
        if (_cachedWindowCoefficients == null || samples.Length != _cachedWindowCoefficients.Length)
        {
            _cachedWindowCoefficients = GetWindowCoefficients(samples.Length);
        }
        var windowedSamples = new List<Complex>(samples.Length);
        for (var i = 0; i < samples.Length; i++)
        {
            windowedSamples.Add(samples[i] * _cachedWindowCoefficients[i]);
        }
        return windowedSamples;
    }

    public abstract double[] GetWindowCoefficients(int width);
}

public class HammingWindow : WindowFunction
{
    public override double[] GetWindowCoefficients(int width)
    {
        var window = new MathNet.Filtering.Windowing.HammingWindow() { Width = width };
        window.Precompute();
        return window.CopyToArray();
    }

}

public class HannWindow : WindowFunction
{
    public override double[] GetWindowCoefficients(int width)
    {
        var window = new MathNet.Filtering.Windowing.HannWindow() { Width = width };
        window.Precompute();
        return window.CopyToArray();
    }
}