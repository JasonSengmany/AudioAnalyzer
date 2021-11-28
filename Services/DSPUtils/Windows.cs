using System.Numerics;

namespace AudioAnalyser.DSPUtils;
public abstract class WindowFunction
{
    private double[]? _cachedWindowCoefficients = null;
    public List<double> ApplyWindow(List<double> samples)
    {
        if (_cachedWindowCoefficients == null || samples.Count() != _cachedWindowCoefficients.Count())
        {
            _cachedWindowCoefficients = GetWindowCoefficients(samples.Count());
        }
        return samples.Zip(_cachedWindowCoefficients, (sample, coeff) => sample * coeff).ToList();
    }

    public List<Complex> ApplyWindow(List<Complex> samples)
    {
        if (_cachedWindowCoefficients == null || samples.Count() != _cachedWindowCoefficients.Count())
        {
            _cachedWindowCoefficients = GetWindowCoefficients(samples.Count());
        }
        for (var i = 0; i < samples.Count(); i++)
        {
            samples[i] *= _cachedWindowCoefficients[i];
        }
        return samples;
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