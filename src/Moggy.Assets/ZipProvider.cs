using System.IO.Compression;

namespace Moggy.Assets;

public sealed class ZipProvider : AssetProvider
{
    private readonly ZipArchive _archive;

    public ZipProvider(Stream stream)
    {
        _archive = new ZipArchive(stream, ZipArchiveMode.Read);
    }

    public override Stream LoadStream(string path)
    {
        var normalizedPath = NormalizePath(path);
        foreach (var entry in _archive.Entries)
        {
            if (!NormalizePath(entry.FullName).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var stream = new MemoryStream();
            using var entryStream = entry.Open();
            entryStream.CopyTo(stream);
            stream.Position = 0;
            return stream;
        }

        throw new FileNotFoundException($"Asset at '{path}' was not found.", path);
    }

    public override void Dispose()
    {
        _archive.Dispose();
        base.Dispose();
    }
}