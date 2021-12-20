
public static class FileHelpers
{
    public static bool IsValidFileSignature(Stream stream, string extension)
    {
        using (var reader = new BinaryReader(stream))
        {
            var signatures = _fileSignature[extension];
            var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

            return signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }
    }
    private static readonly Dictionary<string, List<byte[]>> _fileSignature =
    new Dictionary<string, List<byte[]>>
    {
        { ".wav", new List<byte[]>
            {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }
            }
        },
        { ".flac", new List<byte[]>
            {
                new byte[] { 0x66, 0x4C, 0x61, 0x43, 0x00, 0x00, 0x00, 0x22 }
            }
        },
        {".mp3", new List<byte[]>
            {
                new byte[] { 0x49, 0x44, 0x33}
            }
        }
    };
}
