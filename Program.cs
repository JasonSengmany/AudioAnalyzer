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

// Example pipeline created to extract features from a song
var pipe = new FeatureExtractionPipeline();
pipe.Load(
    new FrequencyBeatDetector(),
    new BasicEnvelopeDetector(),
    new ZeroCrossingRateExtractor(),
    new RootMeanSquareExtractor(),
    new FrequecySpectrogramExtractor(),
    new BandEnergyRatioExtractor(),
    new SpectralCentroidExtractor(),
    new MfccExtractor()
);

var song = new Song(args[0]);
pipe.Process(song);
Console.WriteLine(song);










return 0;