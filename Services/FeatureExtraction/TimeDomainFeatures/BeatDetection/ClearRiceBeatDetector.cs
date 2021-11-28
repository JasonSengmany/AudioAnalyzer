using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.MusicFileReader;
namespace AudioAnalyser.FeatureExtraction;

/// <summary>
/// Class <c>ClearRiceBeatDetector</c> is used to extract the beats per minute from a song
/// based on the implementation described in https://www.clear.rice.edu/elec301/Projects01/beat_sync/beatalgo.html
/// </summary>
public class ClearRiceBeatDetector : BeatDetector
{
    private readonly int _lowerBPM = 60;
    private readonly int _upperBPM = 180;

    private readonly double[] _frequencyBands = { 0, 200, 400, 800, 1600, 3200 };
    private readonly WindowFunction window = new HammingWindow();

    /// <summary>
    /// Performs beat detection using the correlation between comb filters of differing frequencies and 
    /// the song data.
    /// </summary>
    /// <param name="reader">IMusicFileStream for song data access</param>
    /// <returns>Beats per minute</returns>
    protected override int DetectBPM(IMusicFileStream reader)
    {
        // Sample a secion of the song data from the middle of the sample
        // and store the channel data as a complex value
        var musicData = reader.ReadAll();
        var sampleOfInterest = musicData.Skip(musicData.Count / 2)
            .Take((int)(reader.SampleRate * 2.2))
            .Select(x => new Complex(x[0], x[1]))
            .ToList();
        var subbands = PartitionToSubbands(sampleOfInterest, reader.SampleRate);
        EnvelopeDetectSubbands(subbands, reader.SampleRate);
        // // Differentiate the data to accentuate peaks
        // var differentiatedSample = sampleOfInterest.Take(sampleOfInterest.Count() - 1)
        //     .Select((value, index) => reader.SampleRate * (sampleOfInterest[index + 1] - value))
        //     .ToList();

        // // Perform DFT by first windowing the sample and performing FFT
        // window.ApplyWindow(differentiatedSample);
        // var frequencySpectrum = FourierTransform.Radix2FFT(differentiatedSample.ToArray());
        // frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Length / 2 + 1)
        //     .ToArray();

        // var trainOfImpulses = ComputeTrainOfImpulses(ComputePeriods(reader.SampleRate),
        //     differentiatedSample.Count);

        // var energies = new List<double>();

        // foreach (var train in trainOfImpulses)
        // {
        //     energies.Add(train.Zip(frequencySpectrum, (lhs, rhs) => (lhs * rhs).Magnitude).Sum());
        // }

        // var index = energies.IndexOf(energies.Max());
        return 0;
    }

    /// <summary>
    /// This method computes the period T, in terms of sample counts, for each BPM that's to be tested.
    /// </summary>
    /// <param name="sampleRate">Sample rate of song in Hz</param>
    /// <returns>List of periods, in sample counts, for the BPMs in range of 60-80 BPM</returns>
    private List<int> ComputePeriods(int sampleRate)
    {
        var periods = new List<int>();
        for (int i = _lowerBPM; i <= _upperBPM; i += 1)
        {
            periods.Add((int)((60.0 / i) * sampleRate));
        }
        return periods;
    }

    /// <summary>
    /// This method creates a list of FFT transformed comb filters to be convolved with the song. 
    /// </summary>
    /// <param name="periods">List of periods for the BPMs to be tested</param>
    /// <param name="numSamples">Number of samples to be filtered</param>
    /// <returns>List of comb filters with impulses spaced per period</returns>
    private List<Complex[]> ComputeTrainOfImpulses(List<int> periods, int numSamples)
    {
        var trainOfImpulses = new List<Complex[]>();
        foreach (var period in periods)
        {
            var trainOfImpulse = new Complex[numSamples];
            for (var i = 0; i < numSamples; i++)
            {
                if (i % period == 0)
                {
                    trainOfImpulse[i] = new Complex(float.MaxValue, float.MaxValue);
                }
                else
                {
                    trainOfImpulse[i] = Complex.Zero;
                }
            }
            var frequency = FourierTransform.Radix2FFT(window.ApplyWindow(trainOfImpulse.ToList()).ToArray());
            trainOfImpulses.Add(frequency.Take(frequency.Length / 2 + 1).ToArray());
        }
        return trainOfImpulses;
    }

    /// <summary>
    /// This method partitions the sampled signal into frequency subbands with starting frequencies
    /// specified in property <c>_frequencySubbands</c>.
    /// </summary>
    /// <param name="sample">Stereo signal with real component being channel 1 and 
    /// imaginary component being channel 2</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <returns>List of time domain signals for each frequency subband</returns>
    private List<Complex[]> PartitionToSubbands(List<Complex> sample, double sampleRate)
    {
        window.ApplyWindow(sample);
        var frequencySpectrum = FourierTransform.Radix2FFT(sample.ToArray());
        var frequencyStep = sampleRate / frequencySpectrum.Length;
        var bandStartIndexes = _frequencyBands.Select(x => (int)(Math.Floor(x / frequencyStep))).ToArray();
        var subbands = new List<Complex[]>();
        // var plt = new ScottPlot.Plot();
        for (var i = 0; i < bandStartIndexes.Length - 1; i++)
        {
            var frequencySubband = frequencySpectrum.Select(
                (val, index) => index < bandStartIndexes[i]
                || index >= bandStartIndexes[i + 1] ? Complex.Zero : val).ToArray();

            subbands.Add(FourierTransform.Radix2IFFT(frequencySubband));
            // plt.AddSignal(subbands.Last().Select(x => x.Real).ToArray());
            // Console.WriteLine(subbands.Last().Length);
        }
        var lastSubband = frequencySpectrum
            .Select((val, index) => index < bandStartIndexes.Last() ? Complex.Zero : val)
            .ToArray();
        subbands.Add(FourierTransform.Radix2IFFT(lastSubband));
        // plt.AddSignal(subbands.Last().Select(x => x.Magnitude).ToArray());
        // Console.WriteLine(subbands.Last().Length);
        // plt.SaveFig("Divided Subbands.png");
        return subbands;
    }

    /// <summary>
    /// Performs envelope detection on the list of signals.
    /// </summary>
    /// <param name="temporalSubbands">List of frequency subband separated signals in the time domain</param>
    /// <param name="sampleRate"></param>
    /// <returns>List of signal envelopes</returns>
    private List<Complex[]> EnvelopeDetectSubbands(List<Complex[]> temporalSubbands, int sampleRate)
    {
        var halfHannWindow = new HannWindow();
        var windowCoeff = halfHannWindow.GetWindowCoefficients((int)(2 * sampleRate * 0.04))
                              .Skip((int)(sampleRate * 0.04))
                              .ToArray();
        var window = new double[temporalSubbands.First().Length];
        windowCoeff.CopyTo(window, 0);
        var windowFrequencySpectrum = FourierTransform.Radix2FFT(window);

        var plt = new ScottPlot.Plot();
        var smoothedSubbands = new List<Complex[]>();
        foreach (var subband in temporalSubbands)
        {
            var rectifiedSignal = subband
                .Select(x => new Complex(Math.Abs(x.Real), Math.Abs(x.Imaginary))).ToArray();
            var spectrum = FourierTransform.Radix2FFT(rectifiedSignal);
            spectrum = spectrum.Zip(windowFrequencySpectrum, (lhs, coeff) => lhs * coeff).ToArray();
            smoothedSubbands.Add(FourierTransform.Radix2IFFT(spectrum).ToArray());
        }

        plt.AddSignal(temporalSubbands[1].Select(x => x.Magnitude).ToArray());
        plt.AddSignal(smoothedSubbands[1].Select(x => x.Magnitude).ToArray());
        plt.SaveFig("Smoothed.png");
        return smoothedSubbands;
    }

}