using System.Numerics;

namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// This class extracts the spectral centroid (sc) of a song which indicates how "bright" or "dark" a signal appears.
/// </summary>
public class SpectralCentroidExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        if (song.Spectrogram == null)
        {
            throw new FeaturePipelineException("Spectrogram extractor required before band energy ratio extractor");
        }

        song.SpectralCentroids = GetSpectralCentroids(song.Spectrogram, song.FrequencyStep);
        return song;
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