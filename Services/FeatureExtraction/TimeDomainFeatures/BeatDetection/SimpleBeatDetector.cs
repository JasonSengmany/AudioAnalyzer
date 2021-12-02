using System.Text;
using AudioAnalyzer.Services;

namespace AudioAnalyzer.FeatureExtraction;

public class SimpleBeatDetector : BeatDetector
{
    private float _energyThresholdFactor = 1.5f;

    protected override int DetectBPM(IMusicFileStream reader)
    {
        var numSample = 0;
        var beatList = new List<bool>();
        var sample = new float[2];
        var instantEnergyValue = 0f;
        var soundEnergyHistoryBuffer = new Queue<float>();
        var beatString = new StringBuilder();
        while ((sample = reader.ReadNextSampleFrame()) != null)
        {
            instantEnergyValue += sample[0] * sample[0] + ((sample.Count() == 1) ? 0 : sample[1] * sample[1]);
            numSample++;
            if (numSample == _instantBufferLength)
            {
                if (soundEnergyHistoryBuffer.Count == _historyBufferLength)
                {
                    var averageLocalEnergy = soundEnergyHistoryBuffer.Average();
                    var variance = 0f;
                    foreach (var val in soundEnergyHistoryBuffer)
                    {
                        variance += (averageLocalEnergy - val) * (averageLocalEnergy - val);
                    }
                    variance /= _historyBufferLength;
                    // var C = _energyThresholdFactor * variance + 1.5142875f;
                    if (instantEnergyValue > _energyThresholdFactor * averageLocalEnergy)
                    {
                        beatList.Add(true);
                        beatString.Append("|");
                    }
                    else
                    {
                        beatList.Add(false);
                        beatString.Append("-");
                    }
                    soundEnergyHistoryBuffer.Dequeue();
                }
                soundEnergyHistoryBuffer.Enqueue(instantEnergyValue);
                instantEnergyValue = 0f;
                numSample = 0;
            }


        }
        return (int)Math.Floor(CalculateBPM(beatString.ToString(), reader));
    }
    private double CalculateBPM(string beatString, IMusicFileStream reader)
    {
        var measures = beatString.Split('|');
        var validMeasures = measures.Where(s => s.Length > 0);
        var expectedIntervals = validMeasures
            .GroupBy(s => s.Length)
            .OrderByDescending((group) => group.Count());
        Console.WriteLine("----------Detection results----------");
        Console.WriteLine("Detected-BPM,Frequency");
        int total = 0;
        foreach (var group in expectedIntervals)
        {
            var bpm = RescaleBPM((double)(60 / (group.Key * (double)_instantBufferLength / reader.SampleRate)));
            Console.WriteLine("{0,12:0.##}, {1,7}", (decimal)bpm, group.Count());
            total += group.Count();
        }
        Console.WriteLine("Total Intervals: {0,4}", total);
        var expectedIntervalGroup = expectedIntervals.FirstOrDefault();
        if (expectedIntervalGroup == null)
        {
            return 0;
        }
        Console.WriteLine("Prediction Confidence: {0:0.00}%", (double)expectedIntervalGroup.Count() / total * 100);

        //Repeat detection if probability of detected BPM is less than 20%
        if ((double)expectedIntervalGroup.Count() / total * 100 < 20.0 || expectedIntervalGroup.Count() == 1)
        {
            _energyThresholdFactor /= 1.05f;
            reader.Seek(0, SeekOrigin.Begin);
            return DetectBPM(reader);
        }
        var expectedInterval = expectedIntervalGroup.Key;

        //At 44kHz sampling rate a 1024 sample long interval is (1/44000)*1024 seconds long
        //Thus the interval is in total expectedInterval * (1024/44000) seconds long
        var expectedBPM = ConvertIntervalToBpm(expectedInterval, reader.SampleRate);
        expectedBPM = RescaleBPM(expectedBPM);
        return expectedBPM;
    }
    private double ConvertIntervalToBpm(int intervalLength, int sampleRate) => (60d / (intervalLength * (double)_instantBufferLength / sampleRate));
    private double RescaleBPM(double expectedBPM)
    {
        while (expectedBPM < 70 || expectedBPM > 200)
        {
            if (expectedBPM < 70)
            {
                expectedBPM *= 2;
            }
            else
            {
                expectedBPM /= 2;
            }
        }
        return expectedBPM;
    }

}
