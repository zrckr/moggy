using System.IO.Compression;
using Serilog;

namespace Moggy.Assets;

public sealed class ZipProvider : AssetProvider
{
    private static readonly ILogger Logger = Log.ForContext<DirectoryProvider>();

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
                Logger.Debug("Loading asset - {0}", path);
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