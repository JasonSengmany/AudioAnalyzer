using Microsoft.Extensions.DependencyInjection;
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
pipe.Load(new MfccExtractor());
var song = new Song(args[0]);
pipe.Process(song);
Console.WriteLine(song);

// //Using dependency injection to resolve music file reader.
// var services = new ServiceCollection();

// Func<IServiceProvider, IMusicFileStream> readerFactory = Path.GetExtension(args[0]).ToLower() switch
// {
//     ".flac" => (_) => new FlacFileStream(args[0]),
//     ".wav" => (_) => new WaveFileStream(args[0]),
//     ".mp3" => (_) => new MP3FileStream(args[0]),
//     _ => throw new ArgumentException("File type not supported")
// };


// services.AddScoped<IMusicFileStream>(readerFactory);

// services.AddSingleton<BeatDetector, FrequencyBeatDetector>((serviceProvider) =>
//     new FrequencyBeatDetector()
//     {
//         SubbandWidthConstantA = 0.1
//     }
// );

// services.AddSingleton<ZeroCrossingRateExtractor>();

// var serviceProvider = services.BuildServiceProvider();





return 0;