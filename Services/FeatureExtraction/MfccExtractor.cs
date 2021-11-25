using System.Numerics;
using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;
using MathNet.Filtering.Windowing;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using AudioAnalyser.DSPUtils;
public class MfccExtractor : IFeatureExtractor
{
    public int NumMelBands { get; set; } = 64; //increase for more dynamic music

    public int NumMfccs { get; set; } = 30; // Number of Mfccs to retain
    public double LowerFrequencyBound { get; set; } = 300; // Hz
    public double UpperFrequencyBound { get; set; } = 8000; //Hz

    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.MFCC = GetMFCC(reader);
        }
        return song;
    }

    // MFCC definition based off article : 
    // https://vitolavecchia.altervista.org/mel-frequency-cepstral-coefficient-mfcc-guidebook/
    private List<double[]> GetMFCC(IMusicFileStream reader)
    {
        // Calculate initial framing parameters to partition the signal
        var frameLength = (int)(reader.SampleRate * 0.025); // 25ms frame length
        var hopLength = (int)(reader.SampleRate * 0.01);    //10ms hop length

        // Obtain sample frames of the music data.
        var musicData = reader.ReadAll();
        var sampleFrames = PartitionToFrames(musicData, frameLength, hopLength);

        // Construct the mel filter bank based on the number of melbands specified in the NumMelBands property
        // A filter matrix is formed of size (NumMelBands x (FFT Output Length/2+1)) 
        // (Note the second parameter is found by finding the upper power of 2 bound of frameLength, 
        // Then dividing by 2 due to nyquist sampling and adding 1)
        var filterLength = 0;
        if ((frameLength & (frameLength - 1)) == 0)
        {
            filterLength = frameLength / 2 + 1;
        }
        else
        {
            filterLength = (int)(Math.Pow(2, (int)(Math.Log2(frameLength)) + 1) / 2 + 1);
        }
        var melFilter = ConstructMelFilterBands(filterLength, (double)reader.SampleRate / frameLength);

        var mfccs = new List<double[]>();

        foreach (var frame in sampleFrames)
        {
            // Convert to frequency domain by applying windowing function and dft (Short-time fourier transforms)
            var windowedFrame = ApplyWindow(frame);
            var frequencySpectrum = FourierTransform.Radix2FFT(windowedFrame.ToArray());
            // FourierTransformControl.Provider.Forward(frequencySpectrum, FourierTransformScaling.SymmetricScaling);

            //Discard the upper half of the frequency spectrum due to nyquest sampling
            frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray();

            // Obtain the periodogram estimate of the power spectrum
            var powerSpectrum = frequencySpectrum.Select(
                val => 1.0 / frequencySpectrum.Count() * Math.Pow(val.Magnitude, 2)).ToArray();


            // Apply mel-filtering using matrix multiplication with filterbank 
            var powerSpectrumVector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(powerSpectrum);
            var melFilteredPowerSpectrum = melFilter.Multiply(powerSpectrumVector);

            // Take the log of the mel-fitered powers
            var logFilteredPowerSpectrum = melFilteredPowerSpectrum.Select(x => Math.Log10(1 + x));

            // Apply discrete cosine transform to return the MFCCs and only keep the lower coefficients
            var dctResult = logFilteredPowerSpectrum.ToArray();
            FastDctLee.Transform(dctResult);

            mfccs.Add(dctResult.Take(NumMfccs).ToArray());
        }

        return mfccs;
    }

    private List<List<float[]>> PartitionToFrames(List<float[]> musicData, int frameLength, int hopLength)
    {
        var partitions = new List<List<float[]>>();
        for (var offset = 0; offset < musicData.Count() - frameLength; offset += hopLength)
        {
            partitions.Add(musicData.Skip(offset).Take(frameLength).ToList());
        }
        return partitions;
    }

    private List<Complex> ApplyWindow(List<float[]> sampleFrame)
    {
        var window = new HammingWindow() { Width = sampleFrame.Count() };
        window.Precompute();
        return sampleFrame.Zip(window.CopyToArray(),
                            (values, coeff) => new Complex(values[0] * coeff, values[1] * coeff))
                            .ToList();
    }

    private double FrequencyToMel(double frequency)
    {
        return 2595 * Math.Log10(1 + frequency / 700);
    }

    private double MelToFrequency(double mel)
    {
        return 700 * (Math.Pow(10, mel / 2595) - 1);
    }

    private Matrix<double> ConstructMelFilterBands(int numSamples, double frequencyStep)
    {
        var (lowerMel, upperMel) = (FrequencyToMel(LowerFrequencyBound), FrequencyToMel(UpperFrequencyBound));
        var centreMelBands = Generate.LinearSpaced(NumMelBands, lowerMel, upperMel);
        var melStep = centreMelBands[1] - centreMelBands[0];
        var startFreq = MelToFrequency(centreMelBands[0] - melStep);
        var endFreq = MelToFrequency(centreMelBands[NumMelBands - 1] + melStep);
        // Need to round down centre frequencies to discrete steps posiitoned on the sample spectrum.
        var centreFrequencies = centreMelBands
            .Select(mel => MelToFrequency(mel) - MelToFrequency(mel) % frequencyStep).ToList();

        var melFilter = Matrix<double>.Build.Dense(NumMelBands, numSamples);
        for (var i = 0; i < NumMelBands; ++i)
        {
            var lower = i == 0 ? startFreq : centreFrequencies[i - 1];
            var upper = i == NumMelBands - 1 ? endFreq : centreFrequencies[i + 1];
            var filter = ConstructSingleMelBand(lower, centreFrequencies[i], upper, frequencyStep, numSamples);
            melFilter.SetRow(i, filter);
        }
        return melFilter;

    }

    private double[] ConstructSingleMelBand(double lowerFreq, double centreFreq, double upperFreq,
        double freqStep, int filterSize)
    {
        var filter = new double[filterSize];
        for (var index = 0; index < filterSize; index++)
        {
            var currFrequency = index * freqStep;
            if (currFrequency > lowerFreq && currFrequency <= centreFreq)
            {
                filter[index] = (currFrequency - lowerFreq) / (centreFreq - lowerFreq);
            }
            else if (currFrequency > centreFreq && currFrequency <= upperFreq)
            {
                filter[index] = (upperFreq - currFrequency) / (upperFreq - centreFreq);
            }
            else
            {
                filter[index] = 0;
            }
        }
        return filter;
    }

}