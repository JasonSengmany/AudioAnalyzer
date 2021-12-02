
using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;
public class BasicEnvelopeDetector : EnvelopeDetector
{
    protected override List<float> GetAmplitudeEnvelope(IMusicFileStream reader)
    {
        int frameSize = (int)(reader.SampleRate * 0.025); // Approx 25ms frame size
        int hopLength = (int)(reader.SampleRate * 0.01); // Approx 10ms hops
        var musicData = reader.ReadAll();
        var averagedChannelData = musicData.Select(channelData => channelData.Average()).ToArray();
        var amplitudeEnvelope = new List<float>();
        for (var offset = 0; offset < averagedChannelData.Count() - frameSize; offset += hopLength)
        {
            amplitudeEnvelope.Add(averagedChannelData.Skip(offset).Take(frameSize).Max());
        }

        // var plt = new ScottPlot.Plot();
        // plt.AddSignal(Array.ConvertAll(amplitudeEnvelope.ToArray(), x => (double)x));
        // plt.SaveFig("./AmplitudeEnv.png");
        // plt.Clear();
        // plt.AddSignal(Array.ConvertAll(averagedChannelData.ToArray(), x => (double)x));
        // plt.SaveFig("./Signal.png");
        return amplitudeEnvelope;
    }


}

