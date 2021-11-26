using System.Numerics;
using AudioAnalyser.Models;

namespace AudioAnalyser.FeatureExtraction;
public class BandEnergyRatioExtractor : IFeatureExtractor
{
    public float SplitFrequencyHz { get; set; } = 2000;
    public Song ExtractFeature(Song song)
    {
        if (song.Spectrogram == null)
        {
            throw new FeaturePipelineException("Spectrogram extractor required before band energy ratio extractor");
        }
        song.BandEnergyRatio = CalculateBandEnergyRatio(song.Spectrogram, song.FrequencyStep);
        return song;
    }


    private List<double> CalculateBandEnergyRatio(List<Complex[]> spectrogram, double frequencyStep)
    {
        //Determine split index for given sample rate
        var splitIndex = (int)Math.Floor(SplitFrequencyHz / frequencyStep);
        var bandEnergyRatios = new List<double>();
        foreach (var frame in spectrogram)
        {
            // Convert spectrum to power
            var powerSpectrum = frame.Select(sample => Math.Pow(sample.Magnitude, 2)).ToArray();
            //Store the calculated BER at specific frame
            bandEnergyRatios.Add(powerSpectrum.Take(splitIndex).Sum() / powerSpectrum.Skip(splitIndex).Sum());
        }
        return bandEnergyRatios;
    }

}