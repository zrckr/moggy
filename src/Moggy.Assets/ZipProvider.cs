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
            if (NormalizePath(entry.FullName).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Open();
            }
        }

        throw new FileNotFoundException($"Asset at '{path}' was not found.", path);
    }

    public override void Dispose()
    {
        _archive.Dispose();
        base.Dispose();
    }
}