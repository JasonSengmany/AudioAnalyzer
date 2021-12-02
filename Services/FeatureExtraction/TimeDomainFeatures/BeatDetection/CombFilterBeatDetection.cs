using System.Numerics;
using AudioAnalyzer.DSPUtils;
using AudioAnalyzer.Services;
namespace AudioAnalyzer.FeatureExtraction;


/// <summary>
/// Class <c>ComgFilterBeatDetector</c> is used to extract the beats per minute from a song
/// using a series of comb filters convolved with the frequency spectrum of a sample from the song.
/// This extractor is limited to detection in the range of 60-180 BPM and is unaware of potential for 
/// down beats to occur at different pitch. 
/// </summary>
public class CombFilterBeatDetector : BeatDetector
{
    private readonly int _lowerBPM = 60;
    private readonly int _upperBPM = 180;
    private readonly WindowFunction window = new HammingWindow();

    private int _cachedSampleRate = 0;

    private List<Complex[]> _cachedTrainOfImpulses = new();

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
            .Take(FourierTransform.GetNextPowerof2((int)(reader.SampleRate * 5.2)))
            .Select(x => new Complex(x[0], x[1]))
            .ToList();

        // Differentiate the data to accentuate peaks
        var differentiatedSample = sampleOfInterest.Take(sampleOfInterest.Count() - 1)
            .Select((value, index) => reader.SampleRate * (sampleOfInterest[index + 1] - value))
            .ToList();

        // Perform DFT by first windowing the sample and performing FFT
        window.ApplyWindow(differentiatedSample);
        var frequencySpectrum = FourierTransform.Radix2FFT(differentiatedSample.ToArray());
        frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Length / 2 + 1)
            .ToArray();

        if (reader.SampleRate != _cachedSampleRate)
        {
            _cachedSampleRate = reader.SampleRate;
            _cachedTrainOfImpulses = ComputeTrainOfImpulses(ComputePeriods(reader.SampleRate),
            differentiatedSample.Count);
        }

        var energies = new List<double>();
        var plt = new ScottPlot.Plot();
        foreach (var train in _cachedTrainOfImpulses)
        {
            energies.Add(train.Zip(frequencySpectrum, (lhs, rhs) => (lhs * rhs).Magnitude).Sum());
        }

        var index = energies.IndexOf(energies.Max());

        return _lowerBPM + index;
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
            for (var i = 0; i < numSamples; i += period)
            {
                trainOfImpulse[i] = new Complex(short.MaxValue, short.MaxValue);
            }
            var frequency = FourierTransform.Radix2FFT(window.ApplyWindow(trainOfImpulse.ToList()).ToArray());
            trainOfImpulses.Add(frequency.Take(frequency.Length / 2 + 1).ToArray());
        }
        return trainOfImpulses;
    }

}