using System.Numerics;
using AudioAnalyser.DSPUtils;
using AudioAnalyser.Models;

namespace AudioAnalyser.FeatureExtraction;
// Used to characterise how bright a sound is and frequency where most 
// of the signal energy lies.
public class SpectralCentroidExtractor : IFeatureExtractor
{
    public Song ExtractFeature(Song song)
    {
        using (var reader = MusicFileStreamFactory.GetStreamReader(song))
        {
            song.SpectralCentroids = GetSpectralCentroids(reader);
        }
        return song;
    }

    private List<double> GetSpectralCentroids(AudioAnalyser.MusicFileReader.IMusicFileStream reader)
    {
        var frameSize = (int)(reader.SampleRate * 0.025);
        var hopLength = (int)(reader.SampleRate * 0.010);
        var musicData = reader.ReadAll()
            .Select(channelData => new Complex(channelData[0], channelData[1])).ToList();
        var sampleFrames = FourierTransform.PartitionToFrames(musicData, frameSize, hopLength);
        var spectralCentroids = new List<double>(sampleFrames.Count());
        var frequencyStep = (double)reader.SampleRate / FourierTransform.GetNextPowerof2(frameSize);
        var hammingWindow = new HammingWindow();
        foreach (var frame in sampleFrames)
        {
            var windowedFrame = hammingWindow.ApplyWindow(frame);
            //Perform fft on each frame
            var frequencySpectrum = FourierTransform.Radix2FFT(windowedFrame.ToArray());
            frequencySpectrum = frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray();
            var spectrogram = frequencySpectrum.Select(x => x.Magnitude).ToArray();
            var spectralCentroidIndex = (int)Math.Floor(spectrogram.Select((magnitude, index) => magnitude * (index + 1)).Sum() / spectrogram.Sum()) - 1;
            spectralCentroids.Add(frequencyStep * spectralCentroidIndex); // Store the spectral centroid in Hz
        }
        return spectralCentroids;
    }
}