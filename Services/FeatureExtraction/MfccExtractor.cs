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

    private List<double[]> GetMFCC(IMusicFileStream reader)
    {
        var frameLength = 1024;
        var hopLength = reader.SampleRate / 100;
        var musicData = reader.ReadAll();
        var sampleFrames = PartitionToFrames(musicData, frameLength, hopLength);

        var melFilter = ConstructMelFilterBands((int)(frameLength / 2 + 1), (double)reader.SampleRate / frameLength);
        var mfccs = new List<double[]>();
        var spectrogram = Matrix<double>.Build.Dense(sampleFrames.Count(), (int)(frameLength / 2 + 1));
        foreach (var frame in sampleFrames)
        {
            // Convert to frequency domain by applying windowing function and dft
            var windowedFrame = ApplyWindow(frame);
            var frequencySpectrum = windowedFrame.ToArray();
            var result = FourierTransform.Radix2FFT(frequencySpectrum);
            //Discard the upper half of the frequency spectrum due to nyquest sampling
            frequencySpectrum = frequencySpectrum.Take((int)(frameLength / 2 + 1)).ToArray();

            // Apply log transform to get the log spectrum of the signal
            var logAmplitudeSpectra = frequencySpectrum.Select(complex => Math.Log10(1 + complex.Magnitude));

            // Apply mel-filtering using matrix multiplication with filterbank 
            var logAmplitudeSpectraVector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(logAmplitudeSpectra.ToArray());
            var filteredLogAmplitudeSpectrum = melFilter.Multiply(logAmplitudeSpectraVector);

            // Apply discrete cosine transform
            var dctResult = filteredLogAmplitudeSpectrum.ToArray();
            FastDctLee.Transform(dctResult);
            mfccs.Add(dctResult);
        }


        return mfccs;
    }

    private List<List<float[]>> PartitionToFrames(List<float[]> musicData, int frameLength, int hopLength)
    {
        var partitions = new List<List<float[]>>();
        for (var offset = 0; offset < musicData.Count() - hopLength; offset += hopLength)
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
        var plt = new ScottPlot.Plot();
        for (var i = 0; i < NumMelBands; ++i)
        {
            var lower = i == 0 ? startFreq : centreFrequencies[i - 1];
            var upper = i == NumMelBands - 1 ? endFreq : centreFrequencies[i + 1];
            var filter = ConstructSingleMelBand(lower, centreFrequencies[i], upper, frequencyStep, numSamples);
            melFilter.SetRow(i, filter);
            plt.AddSignal(filter);
        }
        plt.SaveFig("./MelFilter.png");
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