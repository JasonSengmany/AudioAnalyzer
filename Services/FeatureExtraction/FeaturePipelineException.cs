using System;

namespace AudioAnalyser.FeatureExtraction;

/// <summary>
/// Thrown when an error with the feature pipeline configuration occurs
/// </summary>
public class FeaturePipelineException : Exception
{
    public FeaturePipelineException()
    {
    }

    public FeaturePipelineException(string message)
        : base(message)
    {
    }

    public FeaturePipelineException(string message, Exception inner)
        : base(message, inner)
    {
    }
}