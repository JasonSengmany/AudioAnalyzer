using AudioAnalyser.FeatureExtraction;
using AudioAnalyser.Models;

if (args.Count() != 1)
{
    Console.WriteLine("Usage: dotnet run </pathToMusicFile>");
    return -1;
}

if (!File.Exists(args[0]))
{
    Console.WriteLine("File does not exists");
    return -1;
}

var pipe = new FeatureExtractionPipeline();
pipe.Load(new MfccExtractor(),
    new FrequencyBeatDetector(),
    new BasicEnvelopeDetector(),
    new FrequecySpectrogramExtractor(),
    new BandEnergyRatioExtractor(),
    new ZeroCrossingRateExtractor(),
    new SpectralCentroidExtractor());

var song = new Song(args[0]);
pipe.Process(song);

// Display f
var plt = new ScottPlot.Plot();
plt.AddSignal(song.ZeroCrossingRates.ToArray());
plt.SaveFig("./ZCR.png");
Console.WriteLine(song);










return 0;