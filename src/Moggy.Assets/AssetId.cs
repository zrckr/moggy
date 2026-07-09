namespace Moggy.Assets;

public readonly record struct AssetId(ulong Id)
{
    public static AssetId Invalid => default;

    public bool IsValid => Id != 0;

    public override string ToString()
    {
        return $"{{{Id}}}";
    }
}