using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct Piece(Cell position)
{
    public Cell Position = position;
    public FaceDirection FacingDirection = FaceDirection.Down;
}

public struct PieceMove(Cell from, Cell to, float speed)
{
    public readonly Cell From = from;
    public readonly Cell To = to;
    public readonly float Speed = speed;
    public TimeSpan Duration;
    public TimeSpan Elapsed;
}

public sealed class PieceSystem : GameSystem
{
    private Query _pieces = null!;

    public override void Startup()
    {
        _pieces = Registry.Query()
            .Include<Piece>()
            .Include<Sprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();

        foreach (var entity in _pieces)
        {
            ref var piece = ref Registry.Get<Piece>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            if (!Registry.Has<PieceMove>(entity))
            {
                // Sync sprite with stationary piece
                sprite.Transform.Position = level.CellToCenter(piece.Position);
                continue;
            }

            // Set up the piece movement
            ref var move = ref Registry.Get<PieceMove>(entity);
            var from = level.CellToCenter(move.From);
            var to = level.CellToCenter(move.To);

            piece.FacingDirection = (to - from).Normalized().ToFaceDirection() ?? FaceDirection.Down;
            move.Elapsed += time.DeltaTimeSpan;

            var distance = move.From.ManhattanDistance(move.To);
            move.Duration = TimeSpan.FromSeconds(distance / move.Speed);
            if (move.Elapsed >= move.Duration)
            {
                // Complete the piece movement
                sprite.Transform.Position = level.CellToCenter(move.To);
                piece.Position = move.To;
                Registry.RemoveDeferred<PieceMove>(entity);
                continue;
            }

            // Process the piece movement
            var amount = Mathz.Clamp01(move.Elapsed.TotalSeconds / move.Duration.TotalSeconds);
            sprite.Transform.Position = Vector2.Lerp(from, to, amount);
            piece.Position = level.WorldToCell(sprite.Transform.Position);
        }
    }
}
