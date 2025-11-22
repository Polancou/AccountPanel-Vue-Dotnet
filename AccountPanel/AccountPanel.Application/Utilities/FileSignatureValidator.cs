namespace AccountPanel.Application.Utilities;

public static class FileSignatureValidator
{
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".jpeg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
            }
        },
        { ".jpg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
            }
        },
        { ".png", new List<byte[]>
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }
        }
    };

    public static bool IsValidImage(Stream fileStream, string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        
        var ext = extension.ToLower();
        if (!_fileSignatures.ContainsKey(ext)) return false;

        fileStream.Position = 0; 

        using var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: true);
        var headerBytes = reader.ReadBytes(_fileSignatures[ext].Max(m => m.Length));

        fileStream.Position = 0; 

        return _fileSignatures[ext].Any(signature => 
            headerBytes.Take(signature.Length).SequenceEqual(signature)
        );
    }
}