using Foster.Framework;

namespace Moggy;

public static class Mathz
{
    public const int TileSize = 8;

    public const int CellSize = TileSize * 2;

    public const int VirtualWidth = TileSize * 30;

    public const int VirtualHeight = TileSize * 40;

    public static readonly RectInt ViewportSize = new(0, CellSize * 2, VirtualWidth, VirtualHeight - CellSize * 3);
}