using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.MusicFileReader;

namespace AudioAnalyser.FeatureExtraction;

public class CombFilterBeatDetector : BeatDetector
{
    private int _lowerBPM = 60;
    private int _upperBPM = 180;

    private WindowFunction window = new HammingWindow();
    protected override int DetectBPM(IMusicFileStream reader)
    {
        var periods = ComputePeriods(reader.SampleRate);
        var musicData = reader.ReadAll();
        var sampleOfInterest = musicData.Skip(musicData.Count / 2).Take((int)(reader.SampleRate * 10.2))
            .Select(x => new Complex(x[0], x[1])).ToList(); // Take 5 seconds of data
        var differentiatedSample = sampleOfInterest.Take(sampleOfInterest.Count() - 1)
            .Select((value, index) => reader.SampleRate * (sampleOfInterest[index + 1] - value)).ToList();
        window.ApplyWindow(differentiatedSample);
        var frequencySpectrum = FourierTransform.Radix2FFT(differentiatedSample.ToArray());
        frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray();
        var trainOfImpulses = ComputeTrainOfImpulses(periods, differentiatedSample.Count());
        var energies = new List<double>();
        foreach (var train in trainOfImpulses)
        {
            energies.Add(train.Zip(frequencySpectrum, (lhs, rhs) => (lhs * rhs).Magnitude).Sum());
        }
        var index = energies.IndexOf(energies.Max());
        return _lowerBPM + index * 2;
    }

    private List<int> ComputePeriods(int sampleRate)
    {
        var periods = new List<int>();
        for (int i = _lowerBPM; i <= _upperBPM; i += 2)
        {
            periods.Add((int)((60.0 / i) * sampleRate));
        }
        return periods;
    }

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
            trainOfImpulses.Add(frequency.Take(frequency.Count() / 2 + 1).ToArray());
        }
        return trainOfImpulses;
    }
}