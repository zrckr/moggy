namespace Moggy.Assets;

public sealed class DirectoryProvider : AssetProvider
{
    private readonly DirectoryInfo _directory;

    public DirectoryProvider(string root)
    {
        _directory = new DirectoryInfo(root);
    }

    public override Stream LoadStream(string path)
    {
        var normalizePath = NormalizePath(path);
        foreach (var file in _directory.EnumerateFiles("*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_directory.FullName, file.FullName);
            if (NormalizePath(relativePath).Equals(normalizePath, StringComparison.OrdinalIgnoreCase))
            {
                return file.OpenRead();
            }
        }

        throw new FileNotFoundException($"Asset at '{path}' was not found.", path);
    }
}