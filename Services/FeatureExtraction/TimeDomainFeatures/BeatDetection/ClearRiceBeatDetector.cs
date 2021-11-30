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

    /// <summary>
    /// Number of frequency subbands equally dividing the sample between 0 and nyquist sampling frequency
    /// </summary>
    private readonly int _numSubbands = 32;
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
                                        .Take((int)(reader.SampleRate * 5.2))
                                        .Select(x => new Complex(x[0], x[1]))
                                        .ToList();

        // Partition the sample into evenly spaced frequency subbands and get their envelopes in the time domain
        var subbandEnvelopes = EnvelopeDetectSubbands(PartitionToSubbands(sampleOfInterest,
            reader.SampleRate), reader.SampleRate);

        //Differentiate the data to accentuate peaks and half-wave rectify (i.e. keep positive values only)
        var differentiatedEnvelopes = new List<Complex[]>();
        foreach (var subband in subbandEnvelopes)
        {
            var differentiatedSubband = subband.Take(sampleOfInterest.Count() - 1)
                .Select((value, index) =>
                {
                    var diff = (subband[index + 1] - value);
                    return new Complex((diff.Real > 0) ? diff.Real : 0,
                                        (diff.Imaginary > 0) ? diff.Imaginary : 0);
                })
                .ToList();
            // window.ApplyWindow(differentiatedSubband);
            differentiatedEnvelopes.Add(FourierTransform.Radix2FFT(differentiatedSubband.ToArray()));
        }

        var trainOfImpulses = ComputeTrainOfImpulses(ComputePeriods(reader.SampleRate),
            differentiatedEnvelopes.First().Length);


        // var plt = new ScottPlot.Plot();
        var energies = new List<double>();
        foreach (var subband in differentiatedEnvelopes)
        {
            var subbandEnergies = new List<double>(trainOfImpulses.Count);
            foreach (var train in trainOfImpulses)
            {
                // plt.AddSignal(subband.Select(x => x.Magnitude).ToArray());
                // // plt.AddSignal(train.Select(x => x.Magnitude).ToArray());
                // plt.SaveFig("Subbandandtrain.png");
                // plt.Clear();
                subbandEnergies.Add(train.Zip(subband, (lhs, rhs) => (lhs * rhs).Magnitude).Sum());
            }

            // plt.AddSignal(subbandEnergies.ToArray());
            // plt.SaveFig("Energies.png");
            Console.WriteLine($"{_lowerBPM + subbandEnergies.IndexOf(subbandEnergies.Max())} {subbandEnergies.Max()}");
            if (energies.Count == 0)
            {
                energies = subbandEnergies;
            }
            else
            {
                energies = energies.Zip(subbandEnergies, (lhs, rhs) => lhs + rhs).ToList();
            }
        }


        var index = energies.IndexOf(energies.Max());
        return _lowerBPM + index;
    }

    /// <summary>
    /// This method partitions the sampled signal into frequency subbands with the number of subbands
    /// specified in property <c>_numSubbands</c>. The subbands must be reflected about the nyquist
    /// frequency in the frequency domain before taking the idft.
    /// </summary>
    /// <param name="sample">Stereo signal with real component being channel 1 and 
    /// imaginary component being channel 2</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <returns>List of time domain signals for each subband</returns>
    private List<Complex[]> PartitionToSubbands(List<Complex> sample, double sampleRate)
    {
        window.ApplyWindow(sample);
        var frequencySpectrum = FourierTransform.Radix2FFT(sample.ToArray());
        var frequencySpectrumSpan = frequencySpectrum.AsSpan();

        var subbandLength = (int)Math.Floor((sampleRate / 2)
                                            / _numSubbands
                                            / (sampleRate / frequencySpectrum.Length));
        var bandStartIndexes = Enumerable.Range(0, _numSubbands)
                                         .Select(x => x * subbandLength)
                                         .ToArray();

        var subbands = new List<Complex[]>();
        // Divide the frequency spectrum into subbands by copying upper and lower halves of the subbands
        for (var i = 0; i < bandStartIndexes.Length - 1; i++)
        {
            var frequencySubband = new Complex[frequencySpectrum.Length];
            var lowerHalfDestination = frequencySubband.AsSpan()
                                                       .Slice(bandStartIndexes[i], subbandLength);
            var upperHalfDestination = frequencySubband.AsSpan()
                                                       .Slice(frequencySubband.Length - bandStartIndexes[i + 1],
                                                       subbandLength);

            frequencySpectrumSpan.Slice(bandStartIndexes[i], subbandLength)
                      .CopyTo(lowerHalfDestination);
            frequencySpectrumSpan.Slice(frequencySubband.Length - bandStartIndexes[i + 1], subbandLength)
                      .CopyTo(upperHalfDestination);

            subbands.Add(FourierTransform.Radix2IFFT(frequencySubband).Take(sample.Count).ToArray());
        }
        var lastSubband = new Complex[frequencySpectrum.Length];
        frequencySpectrumSpan.Slice(bandStartIndexes.Last(), subbandLength * 2)
                             .CopyTo(lastSubband.AsSpan()
                                                .Slice(bandStartIndexes.Last(), subbandLength * 2));

        subbands.Add(FourierTransform.Radix2IFFT(lastSubband).Take(sample.Count).ToArray());
        return subbands;
    }

    /// <summary>
    /// This method computes the period T, in units of samples, for each BPM that's to be tested.
    /// </summary>
    /// <param name="sampleRate">Sample rate of song in Hz</param>
    /// <returns>List of periods, in sample counts, for the BPMs in range of 
    /// <c>_lowerBPM</c> to <c>_upperBPM</c></returns>
    private List<int> ComputePeriods(int sampleRate)
    {
        var periods = new List<int>();
        for (int i = _lowerBPM; i <= _upperBPM; i += 1)
        {
            periods.Add((int)Math.Floor((60.0 / i) * sampleRate));
        }
        return periods;
    }

    /// <summary>
    /// This method creates a list of FFT transformed comb filters to be convolved with the song. 
    /// </summary>
    /// <param name="periods">List of periods for the BPMs to be tested</param>
    /// <param name="numSamples">Number of samples to be filtered</param>
    /// <returns>List of comb filters in frequency domain with impulses spaced per period</returns>
    private List<Complex[]> ComputeTrainOfImpulses(List<int> periods, int numSamples)
    {
        var trainOfImpulses = new List<Complex[]>();
        foreach (var period in periods)
        {
            var trainOfImpulse = new Complex[numSamples];
            for (var i = 0; i < numSamples; i += period)
            {
                trainOfImpulse[i] = new Complex(short.MaxValue, short.MaxValue);
            }
            var frequency = FourierTransform.Radix2FFT(window.ApplyWindow(trainOfImpulse.ToList()).ToArray());
            trainOfImpulses.Add(frequency);
        }
        return trainOfImpulses;
    }

    /// <summary>
    /// Performs envelope detection on the list of signals.
    /// </summary>
    /// <param name="temporalSubbands">List of frequency subband separated signals in the time domain</param>
    /// <param name="sampleRate"></param>
    /// <returns>List of signal envelopes in the time domain</returns>
    private List<Complex[]> EnvelopeDetectSubbands(List<Complex[]> temporalSubbands, int sampleRate)
    {
        var window = GenerateHalfHannWindow(temporalSubbands.First().Length, sampleRate);
        var frequencyDomainFilterCoeff = FourierTransform.Radix2FFT(window);

        var plt = new ScottPlot.Plot();
        var smoothedSubbands = new List<Complex[]>();
        foreach (var subband in temporalSubbands)
        {
            Complex[] spectrum = ApplyFrequencyFilter(frequencyDomainFilterCoeff, subband);
            var timeDomainEnvelope = FourierTransform.Radix2IFFT(spectrum).Take(subband.Length).ToArray();
            smoothedSubbands.Add(timeDomainEnvelope);
        }

        // plt.AddSignal(temporalSubbands[0].Select(x => x.Real).ToArray());
        // plt.AddSignal(temporalSubbands[0].Select(x => x.Imaginary).ToArray());
        // plt.AddSignal(smoothedSubbands[0].Select(x => x.Magnitude).ToArray());
        // plt.SaveFig("Smoothed.png");
        return smoothedSubbands;
    }

    /// <summary>
    /// This method convolves the sample in the frequency domain with the supplied filter coefficients.
    /// The sample is first full-wave rectified to allow removal of higher frequencies during low pass filtering.
    /// </summary>
    /// <param name="frequencyDomainFilterCoefficients"></param>
    /// <param name="timeDomainSamples"></param>
    /// <returns>Filtered samples in frequency domain</returns>
    private Complex[] ApplyFrequencyFilter(Complex[] frequencyDomainFilterCoefficients, Complex[] timeDomainSamples)
    {
        Complex[] rectifiedSignal = FullWaveRectify(timeDomainSamples);
        var spectrum = FourierTransform.Radix2FFT(rectifiedSignal);
        for (int i = 0; i < spectrum.Length; i++)
        {
            spectrum[i] *= frequencyDomainFilterCoefficients[i];
        }
        return spectrum;
    }

    private Complex[] FullWaveRectify(Complex[] timeDomainSamples)
    {
        return timeDomainSamples
                .Select(x => new Complex(Math.Abs(x.Real), Math.Abs(x.Imaginary)))
                .ToArray();
    }

    /// <summary>
    /// This method creates an upper-half hann window of length <c>timeSeconds</c> padded with 0s 
    /// until the <c>fullWindowWidth</c> is reached. 
    /// </summary>
    /// <param name="fullWindowWidth"></param>
    /// <param name="sampleRate"></param>
    /// <returns>Half-hann window coefficients</returns>
    private double[] GenerateHalfHannWindow(int fullWindowWidth, int sampleRate, double timeSeconds = 0.2)
    {
        var halfHannWindow = new HannWindow();
        var windowCoeff = halfHannWindow.GetWindowCoefficients((int)(2 * sampleRate * timeSeconds))
                              .Skip((int)(sampleRate * timeSeconds))
                              .ToArray();
        var window = new double[fullWindowWidth];
        windowCoeff.CopyTo(window, 0);
        return window;
    }


}