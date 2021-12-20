public class FeatureExtractorFormState
{
    private string feature = String.Empty;
    public string Feature
    {
        get { return feature; }
        set
        {
            feature = value;
            NotifyStateChanged();
        }
    }
    public string Extractor { get; set; } = String.Empty;

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}