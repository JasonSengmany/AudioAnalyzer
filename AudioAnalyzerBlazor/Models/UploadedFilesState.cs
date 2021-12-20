
public class UploadResult
{
    public string FileName { get; set; } = default!;
    public string UnsafeFileName { get; set; } = default!;
}
internal class UploadedFilesState
{
    public List<UploadResult> UploadedFiles { get; init; } = new();

    public void AddFile(UploadResult upload)
    {
        UploadedFiles.Add(upload);
        NotifyStateChanged();
    }

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}