using NAudio.Wave;
using NAudio.Dsp;
using System.Text;
using AudioAnalyser.MusicFileReader;

namespace AudioAnalyser.FeatureExtraction;

public class FrequencyBeatDetector : BeatDetector
{
    private int _numSubbands = 64;
    public int NumSubbands
    {
        get { return _numSubbands; }
        set
        {
            _numSubbands = value;
            CalculateSubbandWidths(true);
        }
    }
    private double _subbandWidthConstantA = 0.01;
    public double SubbandWidthConstantA
    {
        get { return _subbandWidthConstantA; }
        set
        {
            _subbandWidthConstantA = value;
            CalculateSubbandWidths(true);
        }
    }
    private double _subbandWidthConstantB;
    public double SubbandWidthConstantB
    {
        get { return _subbandWidthConstantB; }
        set
        {
            _subbandWidthConstantB = value;
            CalculateSubbandWidths(false);
        }
    }
    public List<int> SubbandWidths { get; private set; } = new();

    public float EnergyThreshold { get; set; } = 200f;

    public FrequencyBeatDetector()
    {
        CalculateSubbandWidths(true);
    }

    protected override int DetectBPM(IMusicFileStream reader)
    {
        var numSample = 0;
        var sample = new float[2];
        var instantBuffer = new List<Complex>();
        var energyHistoryBuffer = new List<Queue<float>>();
        var beatString = new StringBuilder();
        for (int i = 0; i < NumSubbands; i++)
        {
            energyHistoryBuffer.Add(new());
        }
        while ((sample = reader.ReadNextSampleFrame()) != null)
        {

            if (numSample == _instantBufferLength)
            {

                var isBeat = CheckIsBeat(instantBuffer, energyHistoryBuffer);
                beatString.Append(isBeat ? "|" : "-");
                numSample = 0;
            }
            instantBuffer.Add(new Complex() { X = sample[0], Y = sample[1] });
            numSample++;

        }
        BeatString = beatString.ToString();
        return (int)Math.Round(CalculateBPM(beatString.ToString(), reader));
    }

    private bool CheckIsBeat(List<Complex> instantBuffer, List<Queue<float>> energyHistoryBuffer)
    {
        var isBeat = false;
        // Calculate the instantaneous audio signal power for each frequency subband
        // Subbands are divided based on linearly increasing sized partitions with 
        // smaller subband widths at lower frequencies.
        var subbandEnergies = CalculateSubbandEnergy(
                    CalculateModulus(CalculateFFT(instantBuffer)));

        if (energyHistoryBuffer[0].Count == _historyBufferLength)
        {
            var averageEnergies = energyHistoryBuffer.Select((subbandHistory, index) =>
                subbandHistory.Average()).ToList();

            // var varianceHistory = new List<float>();
            // for (var i = 0; i < NumSubbands; ++i)
            // {
            //     var variance = 1.0f / NumSubbands * (energyHistoryBuffer[i].Select((value, index) =>
            //            (value - averageEnergies[i]) * (value - averageEnergies[i])).Sum());
            //     Console.WriteLine(variance);
            //     varianceHistory.Add(variance);
            // }

            for (int i = 0; i < NumSubbands; ++i)
            {
                // Console.WriteLine(varianceInHistory[i]);
                isBeat = isBeat || (subbandEnergies[i] > EnergyThreshold * averageEnergies[i]);
                // && varianceInHistory[i] > 50);
            }


            foreach (var q in energyHistoryBuffer)
            {
                q.Dequeue();
            }
        }
        for (int i = 0; i < NumSubbands; ++i)
        {
            energyHistoryBuffer[i].Enqueue(subbandEnergies.ElementAt(i));
        }

        //Reset the instant buffer
        instantBuffer.Clear();
        return isBeat;
    }
    private List<float> CalculateModulus(Complex[] fftRes)
    {
        return fftRes.Select((val, index) => val.X * val.X + val.Y * val.Y).ToList();
    }

    private List<float> CalculateSubbandEnergy(List<float> module)
    {
        var subbandEnergies = new List<float>();
        var toSkip = 0;
        foreach (var width in SubbandWidths)
        {
            subbandEnergies.Add((float)width / _instantBufferLength * module.Skip(toSkip).Take(width).Sum());
            toSkip += width;
        }
        return subbandEnergies;
    }

    private double CalculateBPM(string beatString, IMusicFileStream reader)
    {
        var measures = beatString.Split('|');
        var validMeasures = measures.Where(s => s.Length > 0);
        var expectedIntervals = validMeasures
            .GroupBy(s => s.Length)
            .OrderByDescending((group) => group.Count());
        // Console.WriteLine("----------Detection results----------");
        // Console.WriteLine("Detected-BPM,Frequency");
        int total = 0;
        foreach (var group in expectedIntervals)
        {
            var bpm = RescaleBPM((double)(60 / (group.Key * (double)_instantBufferLength / reader.SampleRate)));
            // Console.WriteLine("{0,12:0.##}, {1,7}", (decimal)bpm, group.Count());
            total += group.Count();
        }
        // Console.WriteLine("Total Intervals: {0,4}", total);
        var expectedIntervalGroup = expectedIntervals.FirstOrDefault();
        if (expectedIntervalGroup == null)
        {
            return 0;
        }
        // Console.WriteLine("Prediction Confidence: {0:0.00}%", (double)expectedIntervalGroup.Count() / total * 100);
        if ((double)expectedIntervalGroup.Count() / total * 100 < 30 && EnergyThreshold > 25)
        {
            EnergyThreshold -= 25;
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

    // Scale the BPM to values between 80 and 200 if it lies outside this range
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
    private Complex[] CalculateFFT(List<Complex> instantBuffer)
    {
        var fftResult = instantBuffer.ToArray();
        FastFourierTransform.FFT(true, (int)Math.Log2((double)_instantBufferLength), fftResult);
        return fftResult;
    }

    private void CalculateSubbandWidths(bool settingA)
    {
        if (settingA)
        {
            _subbandWidthConstantB =
                (_instantBufferLength - _subbandWidthConstantA *
                (NumSubbands * (NumSubbands + 1) / 2)) / NumSubbands;
        }
        else
        {
            _subbandWidthConstantA =
                (_instantBufferLength - NumSubbands * _subbandWidthConstantB)
                / (NumSubbands * (NumSubbands + 1) / 2);
        }
        SubbandWidths.Clear();
        for (var i = 0; i < NumSubbands; i++)
        {
            SubbandWidths.Add((int)Math.Round(_subbandWidthConstantA * (i + 1) + _subbandWidthConstantB));
        }
    }


}