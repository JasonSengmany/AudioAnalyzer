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
            return FFTBody(signal).Select(x => x / Math.Sqrt(signal.Length)).ToArray();

        }
        else
        {
            var nextPowerOf2 = Math.Pow(2, (int)(Math.Log2(signal.Length)) + 1);
            var paddedSignal = new Complex[(int)nextPowerOf2];
            Array.Fill(paddedSignal, Complex.Zero);
            signal.CopyTo(paddedSignal, 0);
            return FFTBody(paddedSignal).Select(x => x / Math.Sqrt(paddedSignal.Length)).ToArray();
        }
    }
    private static Complex[] FFTBody(Complex[] signal)
    {
        var n = signal.Length;
        if (n == 1) return signal;

        var A0 = signal.Where((val, index) => index % 2 == 0).ToArray();
        var A1 = signal.Where((val, index) => index % 2 == 1).ToArray();
        var y0 = FFTBody(A0);
        var y1 = FFTBody(A1);

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

}