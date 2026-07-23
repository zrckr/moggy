using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Mazegen;

namespace Moggy;

public sealed record LevelProperties
{
    public int TilingRows { get; init; }

    public int TilingColumns { get; init; }

    public int CellSize { get; init; }

    public int TargetScore { get; init; }

    public int StartingLives { get; init; }

    public int BugScore { get; init; }

    public int EnemyScore { get; init; }

    public float AbilityOdds { get; init; }

    public int AbilityTrigger { get; init; }

    public TimeSpan RespawnInvincibility { get; init; }

    public required ChaosProperties Chaos { get; init; }

    public required MazeProperties Maze { get; init; }
}

public struct LevelDebug()
{
    public bool ShowTiles = false;
}

public readonly struct Level
{
    public readonly int CellWidth;

    public readonly int CellHeight;

    private readonly Maze _maze;

    public Level(Maze maze, int cellWidth, int cellHeight)
    {
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        _maze = maze;
    }

    public int Rows => _maze.Rows;

    public int Columns => _maze.Columns;

    public int Width => Columns * CellWidth;

    public int Height => Rows * CellHeight;

    public bool Contains(Cell cell)
    {
        return cell is { Row: >= 0, Column: >= 0 } &&
               cell.Column < Columns &&
               cell.Row < Rows;
    }

    public Tile GetTile(Cell cell)
    {
        return _maze[cell.Row, cell.Column];
    }

    public bool IsWalkable(Cell cell)
    {
        return Contains(cell) && GetTile(cell) is Tile.Empty or Tile.Floor or Tile.Exit;
    }

    public Vector2 CellToWorld(Cell cell)
    {
        return new Vector2(
            (-Width / 2f) + (cell.Column * CellWidth),
            (-Height / 2f) + (cell.Row * CellHeight));
    }

    public Vector2 CellToCenter(Cell cell)
    {
        return CellToWorld(cell) + (new Vector2(CellWidth, CellHeight) * 0.5f);
    }

    public Cell WorldToCell(Vector2 position)
    {
        return new Cell(
            (int)MathF.Floor(position.X / CellWidth),
            (int)MathF.Floor(position.Y / CellHeight)
        );
    }
}

public sealed class LevelSystem : GameSystem, IGameSystemGroupState
{
    private LevelProperties _properties = null!;

    private ImageAsset _wall = null!;

    private ImageAsset _debugTiles = null!;

    private Subtexture[] _tileSprites = [];

    private Entity _levelEntity = Entity.Invalid;

    public override void Startup()
    {
        _properties = Assets.LoadJson<LevelProperties>("LevelProperties");
        _wall = Assets.Load<ImageAsset>("GridWall");
        _debugTiles = Assets.Load<ImageAsset>("Level/DebugTiles");
        Registry.Create(new LevelDebug());

        var tileCount = Enum.GetValues<Tile>().Length;
        if (_debugTiles.Width % tileCount != 0)
        {
            throw new InvalidOperationException("The debug tile strip width must be divisible by the tile count.");
        }

        var tileWidth = _debugTiles.Width / tileCount;
        _tileSprites = new Subtexture[tileCount];
        for (var index = 0; index < tileCount; index++)
        {
            _tileSprites[index] = new Subtexture(
                _debugTiles.Texture,
                new Rect(index * tileWidth, 0, tileWidth, _debugTiles.Height));
        }
    }

    public void Enter()
    {
        var region = new List<Point2>();
        for (var row = 0; row < _properties.TilingRows; row++)
        {
            for (var column = 0; column < _properties.TilingColumns; column++)
            {
                region.Add(new Point2(row, column));
            }
        }

        var maze = MazeGenerator.Generate(region, _properties.Maze);
        _levelEntity = Registry.Create(
            new Level(maze, _properties.CellSize, _properties.CellSize));
    }

    public override void Render(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var debug = ref Registry.Singleton<LevelDebug>();

        for (var row = 0; row < level.Rows; row++)
        {
            for (var column = 0; column < level.Columns; column++)
            {
                var cell = new Cell(column, row);
                var tile = level.GetTile(cell);
                if (tile is Tile.Wall or Tile.Corner)
                {
                    Batcher.Image(_wall.Texture, level.CellToWorld(cell), Color.White);
                }

                if (debug.ShowTiles)
                {
                    // Draw the generated tile type over the normal level presentation.
                    var tileSprite = _tileSprites[(int)tile];
                    var scale = new Vector2(
                        level.CellWidth / tileSprite.Width,
                        level.CellHeight / tileSprite.Height);

                    Batcher.Image(tileSprite, level.CellToWorld(cell), Vector2.Zero, scale, 0f,
                        Color.White with { A = 64 });
                }
            }
        }
    }

    public void Exit()
    {
        Registry.Destroy(_levelEntity);
        _levelEntity = Entity.Invalid;
    }

    public override void Shutdown()
    {
        _wall.Dispose();
        _debugTiles.Dispose();
    }
}
