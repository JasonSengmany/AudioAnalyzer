namespace AudioAnalyzer.FeatureExtraction;

/// <summary>
/// Used to indicate possible feature extractors for a given property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FeatureExtractorsAttribute : Attribute
{
    public List<string> PossibleFeatureExtractors { get; init; } = new();
    public FeatureExtractorsAttribute(params string[] featureExtractors)
    {
        PossibleFeatureExtractors.AddRange(featureExtractors);
    }

}


/// <summary>
/// Used to indiicate prerequisite feature extractors for a given extractor.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PrerequisiteExtractorsAttribute : Attribute
{
    public List<string> PrerequisiteExtractors { get; init; } = new();
    public PrerequisiteExtractorsAttribute(params string[] featureExtractors)
    {
        PrerequisiteExtractors.AddRange(featureExtractors);
    }
}