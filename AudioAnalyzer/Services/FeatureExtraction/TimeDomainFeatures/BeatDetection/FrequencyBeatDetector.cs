using NAudio.Wave;
using System.Text;
using AudioAnalyzer.Services;
using System.Numerics;
using AudioAnalyzer.DSPUtils;

namespace AudioAnalyzer.FeatureExtraction;

public class FrequencyBeatDetector : BeatDetector
{
    private int _numSubbands;
    public int NumSubbands
    {
        get { return _numSubbands; }
        set
        {
            _numSubbands = value;
            CalculateSubbandWidths(true);
        }
    }
    private double _subbandWidthConstantA;
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

    public float EnergyThreshold { get; set; }

    public WindowFunction window = new HammingWindow();
    public FrequencyBeatDetector(float energyThreshold = 200, double subbandWidthConstantA = 0.1, int numSubbands = 64)
        => (EnergyThreshold, SubbandWidthConstantA, NumSubbands) = (energyThreshold, subbandWidthConstantA, numSubbands);

    protected override int DetectBPM(IMusicFileStream reader)
    {
        _historyBufferLength = reader.SampleRate / _instantBufferLength; // set to approximately 1 second

        var numSample = 0;
        var sample = new float[2];
        var instantBuffer = new List<Complex>();
        var energyHistoryBuffer = new List<Queue<double>>();
        for (var i = 0; i < NumSubbands; i++)
        {
            energyHistoryBuffer.Add(new());
        }
        var beatString = new StringBuilder();

        while ((sample = reader.ReadNextSampleFrame()) != null)
        {
            if (numSample == _instantBufferLength)
            {

                var isBeat = CheckIsBeat(instantBuffer, energyHistoryBuffer);
                beatString.Append(isBeat ? "|" : "-");
                numSample = 0;
            }
            instantBuffer.Add(sample.Length == 1 ? new Complex(sample[0], 0) : new Complex(sample[0], sample[1]));
            numSample++;

        }
        BeatString = beatString.ToString();
        return (int)Math.Round(CalculateBPM(beatString.ToString(), reader));
    }

    private bool CheckIsBeat(List<Complex> instantBuffer, List<Queue<double>> energyHistoryBuffer)
    {
        var isBeat = false;
        // Calculate the instantaneous audio signal power for each frequency subband
        // Subbands are divided based on linearly increasing sized partitions with 
        // smaller subband widths at lower frequencies.
        var frequencySpectrum = CalculateFFT(instantBuffer);
        var subbandEnergies = CalculateSubbandEnergy(
                    CalculateModulus(frequencySpectrum.Take(frequencySpectrum.Count() / 2 + 1).ToArray()));

        if (energyHistoryBuffer[0].Count == _historyBufferLength)
        {
            var averageEnergies = energyHistoryBuffer.Select((subbandHistory, index) =>
                subbandHistory.Average()).ToList();

            for (int i = 0; i < NumSubbands; ++i)
            {
                isBeat = isBeat || (subbandEnergies[i] > EnergyThreshold * averageEnergies[i]);
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

        instantBuffer.Clear();
        return isBeat;
    }
    private List<double> CalculateModulus(Complex[] fftRes)
    {
        return fftRes.Select((val, index) => Math.Pow(val.Magnitude, 2)).ToList();
    }

    private List<double> CalculateSubbandEnergy(List<double> module)
    {
        var subbandEnergies = new List<double>();
        var toSkip = 0;
        foreach (var width in SubbandWidths)
        {
            subbandEnergies.Add((double)width / _instantBufferLength * module.Skip(toSkip).Take(width).Sum());
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
        if ((double)expectedIntervalGroup.Count() / total * 100 < 30 && EnergyThreshold > 25 || expectedIntervalGroup.Count() == 1)
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
        while (expectedBPM < 60 || expectedBPM > 200)
        {
            if (expectedBPM < 60)
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
        window.ApplyWindow(instantBuffer);
        return FourierTransform.Radix2FFT(instantBuffer.ToArray());
    }

    private void CalculateSubbandWidths(bool settingA)
    {
        if (settingA)
        {
            _subbandWidthConstantB =
                ((_instantBufferLength / 2 + 1) - _subbandWidthConstantA *
                (NumSubbands * (NumSubbands + 1) / 2)) / NumSubbands;
        }
        else
        {
            _subbandWidthConstantA =
                ((_instantBufferLength / 2 + 1) - NumSubbands * _subbandWidthConstantB)
                / (NumSubbands * (NumSubbands + 1) / 2);
        }
        SubbandWidths.Clear();
        for (var i = 0; i < NumSubbands; i++)
        {
            SubbandWidths.Add((int)Math.Round(_subbandWidthConstantA * (i + 1) + _subbandWidthConstantB));
        }
    }


}