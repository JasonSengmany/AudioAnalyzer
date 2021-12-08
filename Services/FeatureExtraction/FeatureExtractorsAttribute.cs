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
/// Used to indiicate prerequisite feature extractor for a given extractor.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PrerequisiteExtractorAttribute : Attribute
{
    public string PrerequisiteExtractor { get; init; } = String.Empty;
    public PrerequisiteExtractorAttribute(string featureExtractor)
    {
        PrerequisiteExtractor = featureExtractor;
    }

    public Stack<string> GetPrerequisiteStack()
    {
        var prerequisiteStack = new Stack<String>();
        prerequisiteStack.Push(PrerequisiteExtractor);
        var nextPrerequisite = PrerequisiteExtractor;
        var prerequisite = Type.GetType($"AudioAnalyzer.FeatureExtraction.{nextPrerequisite}");
        if (prerequisite is null) throw new FeaturePipelineException("Unable to resolve prerequisite");
        var attr = Attribute.GetCustomAttribute(prerequisite, typeof(PrerequisiteExtractorAttribute));
        while (attr != null)
        {
            nextPrerequisite = ((PrerequisiteExtractorAttribute)attr).PrerequisiteExtractor;
            prerequisiteStack.Push(nextPrerequisite);
            prerequisite = Type.GetType($"AudioAnalyzer.FeatureExtraction.{nextPrerequisite}");
            if (prerequisite is null) throw new FeaturePipelineException("Unable to resolve prerequisite");
            attr = Attribute.GetCustomAttribute(prerequisite, typeof(PrerequisiteExtractorAttribute));
        }
        return prerequisiteStack;
    }

}