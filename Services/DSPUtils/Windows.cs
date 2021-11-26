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
        return samples.Zip(_cachedWindowCoefficients, (sample, coeff) => sample * coeff).ToList();
    }

    protected abstract double[] GetWindowCoefficients(int width);
}

public class HammingWindow : WindowFunction
{
    protected override double[] GetWindowCoefficients(int width)
    {
        var window = new MathNet.Filtering.Windowing.HammingWindow() { Width = width };
        window.Precompute();
        return window.CopyToArray();
    }

}