using System.Numerics;
using AudioAnalyzer.DSPUtils;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace AudioAnalyzer.FeatureExtraction;
/// <summary>
/// Class <c>MfccExtractor</c> is used to extract mel-frequency cepstral coefficients from a <c>Song</c>
/// </summary>
[PrerequisiteExtractor(nameof(FrequencySpectrogramExtractor))]
public class MfccExtractor : IFeatureExtractor
{
    /// <value> Property <c>NumMelBands</c> represents the number of mel-scaled frequency bins
    /// This dicates the number of mels used when constructing the mel-filter bank</value>
    public int NumMelBands { get; set; } = 13;

    /// <value> Property <c>NumMfccs</c> represents the number of mfccs to store 
    /// after discrete cosine transformation</value>
    public int NumMfccs { get; set; } = 64;

    /// <value> Property <c>LowerFrequencyBound</c> represents the lower frequency bound in Hz of 
    /// the mel-filter bank</value>
    public double LowerFrequencyBound { get; set; } = 300;

    /// <value> Property <c>LowerFrequencyBound</c> represents the lower frequency bound in Hz of 
    /// the mel-filter bank</value>
    public double UpperFrequencyBound { get; set; } = 10000;

    private int _cachedFilterLength;
    private double _cachedFrequencyStep;
    private Matrix<double> _cachedMelFilter = Matrix<double>.Build.Dense(0, 0);

    private readonly object _asyncProcessLock = new object();


    /// <summary>
    /// This constructor provides default values for the classes properties if not provided, based on generally 
    /// used audio processing parameters.
    /// </summary>
    public MfccExtractor(int numMfccs = 13, int numMelBands = 64, double lowerFrequencyBound = 300,
        double upperFrequencyBound = 10000)
        => (NumMfccs, NumMelBands, LowerFrequencyBound, UpperFrequencyBound) = (numMfccs, numMelBands, lowerFrequencyBound, upperFrequencyBound);

    /// <summary>
    /// Exctracts the MFCC from the <c>song</c> and stores it within <c>song.MFCC<c>
    /// </summary>
    /// <param name="song">Song to be processed</param>
    /// <exception cref="FeaturePipelineException"></exception>
    /// <returns>Reference to the input parameter with the MFCC property set</returns>
    public Song ExtractFeature(Song song)
    {
        if (!song._metadata.ContainsKey("Spectrogram"))
        {
            throw new FeaturePipelineException("Spectrogram is required to extract MFCCs");
        }
        song.MFCC = GetAverageMfccs(GetMFCC((List<Complex[]>)song._metadata["Spectrogram"],
                                            (double)song._metadata["FrequencyStep"]));
        return song;
    }

    /// <summary>
    /// This method extracts the MFCCs based on the definition provided in article:
    /// https://vitolavecchia.altervista.org/mel-frequency-cepstral-coefficient-mfcc-guidebook/
    /// </summary>
    /// <param name="spectrogram">A list of frequency spectrums for each frame in the song</param>
    /// <param name="frequencyStep">The frequency step used in the spectrogram</param>
    /// <returns>A list of MFCCs for each frame found in the spectrogram</returns>
    private List<double[]> GetMFCC(List<Complex[]> spectrogram, double frequencyStep)
    {
        var mfccs = new List<double[]>(spectrogram.Count());
        lock (this)
        {

            if (spectrogram.First().Count() != _cachedFilterLength || frequencyStep != _cachedFrequencyStep)
            {
                _cachedFilterLength = spectrogram.First().Count();
                _cachedFrequencyStep = frequencyStep;
                _cachedMelFilter = ConstructMelFilterBands(_cachedFilterLength, _cachedFrequencyStep);
            }

            foreach (var frame in spectrogram)
            {

                // Obtain the periodogram estimate of the power spectrum
                var powerSpectrum = frame.Select(
                    val => 1.0 / frame.Count() * Math.Pow(val.Magnitude, 2)).ToArray();


                // Apply mel-filtering using matrix multiplication with filterbank 
                var powerSpectrumVector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(powerSpectrum);
                var melFilteredPowerSpectrum = _cachedMelFilter.Multiply(powerSpectrumVector);

                // Take the log of the mel-fitered powers
                var logFilteredPowerSpectrum = melFilteredPowerSpectrum.Select(x => Math.Log10(1 + x));

                // Apply discrete cosine transform to return the MFCCs and only keep the lower coefficients
                var dctResult = logFilteredPowerSpectrum.ToArray();
                FastDctLee.Transform(dctResult);

                mfccs.Add(dctResult.Take(NumMfccs).ToArray());
            }
        }
        return mfccs;
    }

    private double[] GetAverageMfccs(List<double[]> mfccsPerFrame)
    {
        var averageMfccs = new double[NumMfccs];
        foreach (var mfccsInFrame in mfccsPerFrame)
        {
            for (var i = 0; i < NumMfccs; i++)
            {
                averageMfccs[i] += mfccsInFrame[i];
            }
        }
        for (var i = 0; i < NumMfccs; i++)
        {
            averageMfccs[i] /= mfccsPerFrame.Count;
        }
        return averageMfccs;
    }

    /// <summary>
    /// Converts from Hz to mels
    /// </summary>
    /// <param name="frequency">Frequency to convert in Hz</param>
    /// <returns>mel-scaled frequency</returns>
    private double FrequencyToMel(double frequency)
    {
        return 2595 * Math.Log10(1 + frequency / 700);
    }

    /// <summary>
    /// Converts from mels to Hz
    /// </summary>
    /// <param name="mel">Mel to convert to Hz</param>
    /// <returns>Frequency in Hz</returns>
    private double MelToFrequency(double mel)
    {
        return 700 * (Math.Pow(10, mel / 2595) - 1);
    }

    /// <summary>
    /// Constructs a mel-filter bank as a list of triangular filters centred at equally spaced mels 
    /// between the mel-scaled properties <c>LowerFrequencyBound</c> and <c>UpperFrequencyBound</c>
    /// </summary>
    /// <param name="numSamples">The filter length determined by the number of samples in the frequency 
    /// spectrum to be filtered</param>
    /// <param name="frequencyStep">The frequency step between successive points in the frequency 
    /// spectrum to be filtered</param>
    /// <returns>Mel-filter bank matrix of size (<c>NumMelBands</c>,<c>numSamples)</c></returns>
    private Matrix<double> ConstructMelFilterBands(int numSamples, double frequencyStep)
    {
        var (lowerMel, upperMel) = (FrequencyToMel(LowerFrequencyBound), FrequencyToMel(UpperFrequencyBound));
        var centreMelBands = Generate.LinearSpaced(NumMelBands, lowerMel, upperMel);
        var melStep = centreMelBands[1] - centreMelBands[0];
        var startFreq = MelToFrequency(centreMelBands[0] - melStep);
        var endFreq = MelToFrequency(centreMelBands[NumMelBands - 1] + melStep);
        // Need to round down centre frequencies to discrete steps posiitoned on the sample spectrum.
        var centreFrequencies = centreMelBands
            .Select(mel => MelToFrequency(mel) - MelToFrequency(mel) % frequencyStep).ToList();

        var melFilter = Matrix<double>.Build.Dense(NumMelBands, numSamples);
        for (var i = 0; i < NumMelBands; ++i)
        {
            var lower = i == 0 ? startFreq : centreFrequencies[i - 1];
            var upper = i == NumMelBands - 1 ? endFreq : centreFrequencies[i + 1];
            var filter = ConstructSingleMelBand(lower, centreFrequencies[i], upper, frequencyStep, numSamples);
            melFilter.SetRow(i, filter);
        }
        return melFilter;

    }

    /// <summary>
    /// This method creates a single triangular filter which makes up the mel-filter bank
    /// </summary>
    /// <param name="lowerFreq"></param>
    /// <param name="centreFreq"></param>
    /// <param name="upperFreq"></param>
    /// <param name="freqStep"></param>
    /// <param name="filterSize"></param>
    /// <returns>Single row of the mel filter bank</returns>
    private double[] ConstructSingleMelBand(double lowerFreq, double centreFreq, double upperFreq,
                                            double freqStep, int filterSize)
    {
        var filter = new double[filterSize];
        for (var index = 0; index < filterSize; index++)
        {
            var currFrequency = index * freqStep;
            if (currFrequency > lowerFreq && currFrequency <= centreFreq)
            {
                filter[index] = (currFrequency - lowerFreq) / (centreFreq - lowerFreq);
            }
            else if (currFrequency > centreFreq && currFrequency <= upperFreq)
            {
                filter[index] = (upperFreq - currFrequency) / (upperFreq - centreFreq);
            }
            else
            {
                filter[index] = 0;
            }
        }
        return filter;
    }

}