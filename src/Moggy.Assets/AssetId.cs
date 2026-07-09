namespace Moggy.Assets;

public readonly record struct AssetId(ulong Id)
{
    public override string ToString()
    {
        if (Id == 0)
        {
            return "0";
        }

        Span<char> buffer = stackalloc char[13];
        var value = Id;
        var index = buffer.Length;

        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        while (value > 0)
        {
            buffer[--index] = alphabet[(int)(value % (ulong)alphabet.Length)];
            value /= (ulong)alphabet.Length;
        }

        return buffer[index..].ToString();
    }
}