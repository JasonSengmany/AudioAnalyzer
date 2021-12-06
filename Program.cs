using AudioAnalyzer.FeatureExtraction;
using AudioAnalyzer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;

if (args.Count() == 0 || !File.Exists(args[0]) && !Directory.Exists(args[0]))
{
    Console.WriteLine("Usage: AudioAnalyzer.exe </pathToMusicFileOrDirectory> [OPTIONS]+");
    return -1;
}

var savePath = "results.csv";
var shouldShowHelp = false;
var extras = new List<string>();
var options = new OptionSet {
    {"o|output=","Specify the output file path. Existing files will be overwritten.",v => savePath = v},
    {"h|help","Show help message",h => shouldShowHelp = h!=null}

};

try
{
    extras = options.Parse(args);
}
catch (OptionException e)
{
    Console.WriteLine(e);
    Console.WriteLine("Usage: AudioAnalyzer.exe </pathToMusicFileOrDirectory> [OPTIONS]+");
    return -1;
}

if (shouldShowHelp)
{
    Console.WriteLine("Usage: AudioAnalyzer.exe </pathToMusicFileOrDirectory> [OPTIONS]+");
    Console.WriteLine("Extract features of songs specified in the file or directory.");
    Console.WriteLine("Results are saved to disk in ./results.json unless specified otherwise.");
    Console.WriteLine();
    Console.WriteLine("Options:");
    options.WriteOptionDescriptions(Console.Out);

    return 0;
}


var services = new ServiceCollection();

services.AddSingleton<AudioAnalyzerController>();

services.AddSingleton<FeatureExtractionPipeline>((serviceProvider) =>
    new FeatureExtractionPipeline(
        new DirectoryLabelExtractor(),
        new ZeroCrossingRateExtractor()
    )
);


switch (Path.GetExtension(savePath))
{
    case (".db"):
        services.AddSqlite<SongDbContext>($"Data Source={savePath}");
        services.AddScoped<IPersistenceService, SqlitePersistenceService>();
        break;
    case (".csv"):
        services.AddScoped<IPersistenceService, CsvPersistenceService>();
        break;
    case (".json"):
        services.AddScoped<IPersistenceService, JsonPersistenceService>();
        break;
    default:
        throw new ArgumentException("Unsupported file type requested for result output. Please specify either json or csv");
}

var serviceProvider = services.BuildServiceProvider();
var context = serviceProvider.GetService<SongDbContext>();
if (context != null) context.Database.Migrate();

var controller = serviceProvider.GetRequiredService<AudioAnalyzerController>();
var songs = controller.LoadSongs(args[0]);
var watch = Stopwatch.StartNew();
await controller.ProcessFeaturesAsync();
Console.WriteLine(watch.ElapsedMilliseconds);
await controller.SaveFeatures(savePath);

return 0;


