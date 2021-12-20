using Microsoft.AspNetCore.Mvc;
using AudioAnalyzer.Models;
using System.Text.Json;
using AudioAnalyzer.Services;
using AudioAnalyzer.FeatureExtraction;
namespace AudioAnalyzerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioFeatureController : ControllerBase
{
    public const string SessionKeySelectedExtractors = "_Extractors";
    private readonly ILogger<AudioFeatureController> _logger;
    private readonly AudioAnalyzerService _audioAnalyzerService;
    public AudioFeatureController(ILogger<AudioFeatureController> logger, AudioAnalyzerService AudioAnalyzerService)
    {
        _logger = logger;
        _audioAnalyzerService = AudioAnalyzerService;
    }

    [HttpGet("features", Name = "GetAllFeatures")]
    public ActionResult<IEnumerable<string>> GetPossibleFeatures()
    {
        return typeof(Song).GetProperties().Select(property => property.Name).ToArray();
    }

    [HttpGet("extractors", Name = "GetAllExtractors")]
    public ActionResult<IEnumerable<string>> GetAllExtractors()
    {
        var propertyInfos = typeof(Song).GetProperties();
        var extractors = new List<string>();
        foreach (var property in propertyInfos)
        {
            var attr = Attribute.GetCustomAttribute(property, typeof(FeatureExtractorsAttribute));
            if (attr != null)
            {
                extractors.AddRange(((FeatureExtractorsAttribute)attr).PossibleFeatureExtractors);
            }
        }
        return extractors;
    }


    [HttpGet("extractors/{feature}", Name = "GetExtractorsForFeature")]
    public ActionResult<IEnumerable<string>> GetFeatureExtractors([FromQuery] string feature)
    {
        var propertyInfo = typeof(Song).GetProperty(feature);
        if (propertyInfo is null)
        {
            return NotFound();
        }
        var attr = (FeatureExtractorsAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(FeatureExtractorsAttribute));
        return Ok(attr.PossibleFeatureExtractors);
    }


    [HttpPost("analyse", Name = "AnalyseSong")]
    [RequestSizeLimit(40L * 1024L * 1024L)]
    [RequestFormLimits(MultipartBodyLengthLimit = 40L * 1024L * 1024L)]
    public async Task<IActionResult> PostFileAnalyseAsync(IFormFile file, [FromQuery] string[] extractors)
    {
        var filePath = String.Empty;
        if (file.Length > 0)
        {
            foreach (var extractor in extractors)
            {
                if (_audioAnalyzerService.FeatureExtractionPipeline.TryLoad(extractor) == false)
                {
                    return NotFound();
                }
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            filePath = $"{Path.GetTempFileName()}{ext}";
            if (string.IsNullOrEmpty(ext) || !MusicFileStreamFactory.SupportedFormats.Contains(ext)
            || !FileHelpers.IsValidFileSignature(file.OpenReadStream(), ext))
            {
                return new UnsupportedMediaTypeResult();
            }

            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            _audioAnalyzerService.LoadSongs(filePath);
        }
        _logger.LogInformation($"Extracting features using {string.Join(", ", extractors)}");
        var processedSong = await _audioAnalyzerService.ProcessFeaturesAsync();
        System.IO.File.Delete(filePath);
        return Ok(processedSong);
    }


}

public static class SessionExtensions
{
    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}
