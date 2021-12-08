using System.Numerics;

namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// This class extracts the spectral centroid (sc) of a song which indicates how "bright" or "dark" a signal appears.
/// </summary>
[PrerequisiteExtractors("FrequencySpectrogramExtractor")]
public class SpectralCentroidExtractor : PrerequisiteExtractor
{
    public SpectralCentroidExtractor(params IFeatureExtractor[] dependentExtractors)
        : base(dependentExtractors) { }

    protected override void PreFeatureExtraction(Song song)
    {
        if (!song._metadata.ContainsKey("Spectrogram"))
        {
            throw new FeaturePipelineException("Spectrogram extractor required before band energy ratio extractor");
        }
        song._metadata.Add("SpectralCentroids", GetSpectralCentroids((List<Complex[]>)song._metadata["Spectrogram"],
                                                            (double)song._metadata["FrequencyStep"]));
        song.AverageSpectralCentroid = ((List<double>)song._metadata["SpectralCentroids"]).Average();
    }

    protected override void PostFeatureExtraction(Song song)
    {
        song._metadata.Remove("SpectralCentroids");

    }

    private List<double> GetSpectralCentroids(List<Complex[]> spectrogram, double frequencyStep)
    {
        var spectralCentroids = new List<double>(spectrogram.Count());
        foreach (var frame in spectrogram)
        {
            var magnitudes = frame.Select(x => x.Magnitude).ToArray();
            var spectralCentroidIndex = (int)Math.Floor(magnitudes.Select((magnitude, index) => magnitude * (index + 1)).Sum() / magnitudes.Sum()) - 1;
            spectralCentroids.Add(frequencyStep * spectralCentroidIndex);
        }
        return spectralCentroids;
    }
}