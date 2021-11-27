using System.Numerics;

namespace AudioAnalyser.DSPUtils;
internal sealed class FourierTransform
{

    // Radix-2 FFT implemented based on algorithm explained in 
    //"Introduction to Algorithms" by Cormen, Leiserson, Rivest, and Stein
    // For signals of length not of a power of 2, padding is performed.
    public static Complex[] Radix2FFT(Complex[] signal)
    {
        if ((signal.Length & (signal.Length - 1)) == 0)
        {
            var normFactor = Math.Sqrt(signal.Length);
            return FFTBody(signal).Select(x => x / normFactor).ToArray();

        }
        else
        {
            var nextPowerOf2 = Math.Pow(2, (int)(Math.Log2(signal.Length)) + 1);
            var paddedSignal = new Complex[(int)nextPowerOf2];
            Array.Fill(paddedSignal, Complex.Zero);
            signal.CopyTo(paddedSignal, 0);
            var normFactor = Math.Sqrt(paddedSignal.Length);
            return FFTBody(paddedSignal).Select(x => x / normFactor).ToArray();
        }
    }

    public static Complex[] Radix2FFT(double[] signal)
    {
        return Radix2FFT(signal.Select(x => new Complex(x, 0)).ToArray());
    }
    private static Complex[] FFTBody(Complex[] signal)
    {
        var n = signal.Length;
        if (n == 1) return signal;

        var y0 = FFTBody(signal.Where((val, index) => index % 2 == 0).ToArray());
        var y1 = FFTBody(signal.Where((val, index) => index % 2 == 1).ToArray());

        var wn = Complex.Exp(new Complex(0, -2 * Math.PI / n));
        var w = Complex.One;

        var y = new Complex[n];
        for (var k = 0; k <= n / 2 - 1; k++)
        {
            y[k] = y0[k] + w * y1[k];
            y[n / 2 + k] = y0[k] - w * y1[k];
            w = w * wn;
        }
        return y;
    }

    // Brute force FFT presented in https://www.nayuki.io/page/how-to-implement-the-discrete-fourier-transform
    public static Complex[] computeDft(Complex[] input)
    {
        int n = input.Length;
        Complex[] output = new Complex[n];
        for (int k = 0; k < n; k++)
        {  // For each output element
            Complex sum = 0;
            for (int t = 0; t < n; t++)
            {  // For each input element
                double angle = 2 * Math.PI * t * k / n;
                sum += input[t] * Complex.Exp(new Complex(0, -angle));
            }
            output[k] = sum;
        }
        return output;
    }
    public static List<List<T>> PartitionToFrames<T>(List<T> musicData, int frameLength, int hopLength)
    {
        var partitions = new List<List<T>>();
        for (var offset = 0; offset < musicData.Count() - frameLength; offset += hopLength)
        {
            partitions.Add(musicData.Skip(offset).Take(frameLength).ToList());
        }
        return partitions;
    }

    public static int GetNextPowerof2(int frameLength)
    {
        var filterLength = 0;
        if ((frameLength & (frameLength - 1)) == 0)
        {
            filterLength = frameLength;
        }
        else
        {
            filterLength = (int)(Math.Pow(2, (int)(Math.Log2(frameLength)) + 1));
        }
        return filterLength;
    }
}