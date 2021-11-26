using System.Numerics;
using AudioAnalyser.Models;

namespace AudioAnalyser.FeatureExtraction;

/// <summary>
/// This class is used to extract the band energy ratios (BER) which indicates how much lower frequencies dominate the signal.
/// A spectrogram is expected to have been extracted from the song and dictates the framelength and hoplength of the 
/// band energy ratio extraction.
/// </summary>
public class BandEnergyRatioExtractor : IFeatureExtractor
{
    /// <summary>
    /// This value indicates the division between the higher and lower frequencies used in the BER calculation.
    /// </summary>
    /// <value>A default of 1000Hz is provided which is found in the lower midrange where vocals may appear</value>
    public float SplitFrequencyHz { get; set; }

    public BandEnergyRatioExtractor(float splitFrequency = 1000) => SplitFrequencyHz = splitFrequency;

    /// <summary>
    /// Extracts the band energy ratio and stores it in the property <c>song.BandEnergyRatios</c>
    /// </summary>
    /// <param name="song"></param>
    /// <exception cref="FeaturePipelineException">Requires a spectrogram extracted from the song</exception>
    /// <returns>Reference to input song with property BandEnergyRatios set</returns>
    public Song ExtractFeature(Song song)
    {
        if (song.Spectrogram == null)
        {
            throw new FeaturePipelineException("Spectrogram extractor required before band energy ratio extractor");
        }
        song.BandEnergyRatios = CalculateBandEnergyRatio(song.Spectrogram, song.FrequencyStep);
        return song;
    }

    /// <summary>
    /// This method calculates the band energy ratios for each frame of the spectrogram.
    /// </summary>
    /// <param name="spectrogram">The list of frequency spectrum frames extracted by class 
    /// <c>SpectrogramExtractor</c></param>
    /// <param name="frequencyStep"></param>
    /// <returns>List of band energy ratios for each frame in the spectrogram</returns>
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