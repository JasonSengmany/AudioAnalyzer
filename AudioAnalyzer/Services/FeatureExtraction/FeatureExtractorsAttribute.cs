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

    /// <summary>
    /// Obtain a stack of prequisite extractors with the top of the stack being the root prerequisite
    /// while the bottom is the immediate parent prerequisite extractor.
    /// </summary>
    /// <returns>Stack of extractor names</returns>
    /// <exception cref="FeaturePipelineException"></exception>
    public Stack<string> GetPrerequisiteStack()
    {
        var prerequisiteStack = new Stack<String>();
        prerequisiteStack.Push(PrerequisiteExtractor);
        var nextPrerequisite = PrerequisiteExtractor;
        var prerequisite = Type.GetType($"AudioAnalyzer.FeatureExtraction.{nextPrerequisite}");
        if (prerequisite is null) throw new FeaturePipelineException($"Unable to resolve prerequisite {nextPrerequisite}");
        var attr = Attribute.GetCustomAttribute(prerequisite, typeof(PrerequisiteExtractorAttribute));
        while (attr != null)
        {
            nextPrerequisite = ((PrerequisiteExtractorAttribute)attr).PrerequisiteExtractor;
            prerequisiteStack.Push(nextPrerequisite);
            prerequisite = Type.GetType($"AudioAnalyzer.FeatureExtraction.{nextPrerequisite}");
            if (prerequisite is null) throw new FeaturePipelineException($"Unable to resolve prerequisite {nextPrerequisite}");
            attr = Attribute.GetCustomAttribute(prerequisite, typeof(PrerequisiteExtractorAttribute));
        }
        return prerequisiteStack;
    }
    public static Stack<string> GetPrerequisites(IFeatureExtractor featurizer)
    {
        var attribute = Attribute.GetCustomAttribute(featurizer.GetType(), typeof(PrerequisiteExtractorAttribute));
        if (attribute is null) return new();
        return ((PrerequisiteExtractorAttribute)attribute).GetPrerequisiteStack();
    }
}