using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public struct Grid(int width, int height, int cellWidth, int cellHeight)
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly int CellWidth = cellWidth;
    public readonly int CellHeight = cellHeight;
}

public sealed class GridSystem : GameSystem
{
    private Query _grid = null!;

    private ImageAsset _gridCell = null!;

    public override void Startup()
    {
        _gridCell = Assets.Load<ImageAsset>("GridCell");
        _grid = Registry.Query()
            .Include<Grid>()
            .Build();
    }

    public override void Render(Time time)
    {
        var entity = _grid.Single();
        ref var grid = ref Registry.Get<Grid>(entity);

        var halfHeight = grid.Height / 2;
        var halfWidth = grid.Width / 2;
        for (var y = -halfHeight; y < halfHeight; y += grid.CellHeight)
        {
            for (var x = -halfWidth; x < halfWidth; x += grid.CellWidth)
            {
                Batcher.Image(_gridCell.Texture, new Vector2(x, y), Color.DarkGray);
            }
        }
    }

    public override void Shutdown()
    {
        _gridCell.Dispose();
    }
}