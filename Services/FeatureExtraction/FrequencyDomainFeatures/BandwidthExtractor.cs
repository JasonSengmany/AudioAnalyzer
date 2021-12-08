using System.Numerics;

namespace AudioAnalyzer.FeatureExtraction;

[PrerequisiteExtractor(nameof(SpectralCentroidExtractor))]
public class BandwidthExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        if (!song._metadata.ContainsKey("SpectralCentroids") || !song._metadata.ContainsKey("Spectrogram"))
        {
            throw new FeaturePipelineException("Spectral centroids and spectrogram are required to calculate bandwidth");
        }
        song.AverageBandwidth = GetBandwidths((List<Complex[]>)song._metadata["Spectrogram"],
                                              (List<double>)song._metadata["SpectralCentroids"],
                                              (double)song._metadata["FrequencyStep"]).Average();
        return song;
    }

    private List<double> GetBandwidths(List<Complex[]> spectrogram,
                                       List<double> spectralCentroids,
                                       double frequencyStep)
    {
        var bandwidths = new List<double>();
        for (var frameIndex = 0; frameIndex < spectrogram.Count; frameIndex++)
        {
            var weightedDistance = 0.0;
            var sumOfWeights = 0.0;
            for (var frequencyIndex = 0; frequencyIndex < spectrogram[frameIndex].Length; frequencyIndex++)
            {
                var magnitude = spectrogram[frameIndex][frequencyIndex].Magnitude;
                weightedDistance += Math.Abs(frequencyIndex * frequencyStep - spectralCentroids[frameIndex]) * magnitude;
                sumOfWeights += magnitude;
            }
            bandwidths.Add(weightedDistance / sumOfWeights);
        }
        return bandwidths;
    }
}