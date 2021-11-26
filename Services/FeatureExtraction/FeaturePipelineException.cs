using System;

namespace AudioAnalyser.FeatureExtraction;
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