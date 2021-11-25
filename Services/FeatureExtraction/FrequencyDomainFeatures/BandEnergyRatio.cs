using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;
using AudioAnalyser.MusicFileReader;

public class BandEnergyRatioExtractor : IFeatureExtractor
{
    public float SplitFrequencyHz { get; set; } = 2000;
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.BandEnergyRatio = CalculateBandEnergyRatio(reader);
        }
        return song;
    }

    private List<double> CalculateBandEnergyRatio(IMusicFileStream reader)
    {
        var frameSize = (int)(reader.SampleRate * 0.025);
        var hopLength = (int)(reader.SampleRate * 0.010);
        var musicData = reader.ReadAll();
        var sampleFrames = FourierTransform.PartitionToFrames(musicData, frameSize, hopLength);

        //Determine split index for given sample rate
        var frequencyStep = (double)reader.SampleRate / FourierTransform.GetNextPowerof2(frameSize);
        var splitIndex = (int)Math.Floor(SplitFrequencyHz / frequencyStep);

        var bandEnergyRatios = new List<double>();
        foreach (var frame in sampleFrames)
        {
            //Convert frame to complex array
            var frameData = frame.Select(channelData => new Complex(channelData[0], channelData[1])).ToArray();
            //Perform fft on the sample frame:
            var frequencySpectrum = FourierTransform.Radix2FFT(frameData);
            //Discard upper half of the result;
            frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray();
            // Convert spectrum to power
            var powerSpectrum = frequencySpectrum.Select(sample => Math.Pow(sample.Magnitude, 2)).ToArray();

            //Store the calculated BER at specific frame
            bandEnergyRatios.Add(powerSpectrum.Take(splitIndex).Sum() / powerSpectrum.Skip(splitIndex).Sum());
        }
        return bandEnergyRatios;
    }


}