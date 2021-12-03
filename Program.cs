using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;


if (args.Count() != 1)
{
    Console.WriteLine("Usage: dotnet run </pathToMusicFileOrDirectory>");
    return -1;
}

if (!File.Exists(args[0]) && !Directory.Exists(args[0]))
{
    Console.WriteLine("File or directory does not exists");
    return -1;
}

var services = new ServiceCollection();

services.AddSingleton<AudioAnalyzerController>();

services.AddScoped<IPersistenceService, JsonPersistenceService>();

services.AddSingleton<FeatureExtractionPipeline>((serviceProvider) =>
new FeatureExtractionPipeline(
    new CombFilterBeatDetector(),
    new FrequecySpectrogramExtractor(
        new SpectralCentroidExtractor(),
        new BandEnergyRatioExtractor(),
        new MfccExtractor()
    )
));

var serviceProvider = services.BuildServiceProvider();

var controller = serviceProvider.GetRequiredService<AudioAnalyzerController>();

var songs = controller.LoadSongs(args[0]);
controller.ProcessFeatures();
await controller.SaveFeatures("./test.json");
foreach (var song in controller.Songs)
{
    Console.WriteLine(song);
}

return 0;
