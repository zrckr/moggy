using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public enum PlayerState
{
    Idle,
    Move
}

public struct Player()
{
    public PlayerState State
    {
        get;
        set
        {
            if (field != value)
            {
                PreviousState = field;
                field = value;
            }
        }
    } = PlayerState.Idle;

    public PlayerState PreviousState { get; private set; } = PlayerState.Idle;

    public FaceDirection Direction = FaceDirection.Down;

    public FaceDirection? BufferedDirection = null;
}

public sealed class PlayerSystem : GameSystem
{
    private const float CellMoveDuration = 0.15f;

    private Query _grid = null!;

    private Query _player = null!;

    private VirtualDevice _inputDevice = null!;

    private VirtualStick _move = null!;

    private SpriteAsset _idleSprite = null!;

    private SpriteAsset _moveSprite = null!;

    public override void Startup()
    {
        _grid = Registry.Query()
            .Include<Grid>()
            .Build();

        _idleSprite = Assets.Load<SpriteAsset>("Player/Idle");
        _moveSprite = Assets.Load<SpriteAsset>("Player/Move");

        var gridEntity = _grid.Single();
        ref var grid = ref Registry.Get<Grid>(gridEntity);
        var startCell = new Point2(grid.Columns / 2, grid.Rows / 2);

        var player = Registry.Create();
        Registry.Set(player, new Player());
        Registry.Set(player, new GridPosition
        {
            Cell = startCell
        });
        Registry.Set(player, new Transform
        {
            Position = grid.CellToCenter(startCell),
            Scale = new Vector2(2f)
        });
        Registry.Set(player, new AnimatedSprite
        {
            Animation = FaceDirection.Down.GetAnimationName(),
            Sprite = _idleSprite
        });

        _inputDevice = new VirtualDevice(App.Input, "Player");
        _inputDevice.IndexMode = VirtualDevice.IndexModes.AutomaticLatest;
        _move = _inputDevice.AddStick("Move",
            new StickBindingSet()
                .AddArrowKeys());

        _player = Registry.Query()
            .Include<Player>()
            .Include<GridPosition>()
            .Include<Transform>()
            .Include<AnimatedSprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        var playerEntity = _player.Single();
        var gridEntity = _grid.Single();
        ref var grid = ref Registry.Get<Grid>(gridEntity);
        ref var player = ref Registry.Get<Player>(playerEntity);
        ref var gridPosition = ref Registry.Get<GridPosition>(playerEntity);
        ref var animated = ref Registry.Get<AnimatedSprite>(playerEntity);

        if (Registry.Has<GridMover>(playerEntity))
        {
            if (TryReadPressedDirection(out var pressed))
            {
                player.BufferedDirection = pressed;
            }

            player.State = PlayerState.Move;
            animated.Sprite = _moveSprite;
        }
        else
        {
            player.State = PlayerState.Idle;

            if (TryChooseMoveDirection(ref player, in grid, in gridPosition, out var direction) &&
                TryStartMove(playerEntity, in grid, in gridPosition, direction))
            {
                player.State = PlayerState.Move;
                animated.Sprite = _moveSprite;
            }
            else
            {
                animated.Sprite = _idleSprite;
            }
        }

        animated.Animation = player.Direction.GetAnimationName();
        animated.FlipHorizontal = player.Direction.IsAnimationFlipped();
    }

    private bool TryStartMove(Entity entity, in Grid grid, in GridPosition gridPosition, FaceDirection direction)
    {
        var target = gridPosition.Cell + direction.ToPoint2();
        if (!grid.Contains(target))
        {
            return false;
        }

        Registry.Set(entity, new GridMover
        {
            From = gridPosition.Cell,
            To = target,
            Progress = 0f,
            Speed = 1f / CellMoveDuration
        });

        return true;
    }

    private bool TryChooseMoveDirection(
        ref Player player,
        in Grid grid,
        in GridPosition gridPosition,
        out FaceDirection direction)
    {
        if (!TryReadBufferedOrHeldDirection(ref player, out var requested))
        {
            direction = default;
            return false;
        }

        player.Direction = requested;

        if (!grid.Contains(gridPosition.Cell + requested.ToPoint2()))
        {
            direction = default;
            return false;
        }

        direction = requested;
        return true;
    }

    private bool TryReadBufferedOrHeldDirection(ref Player player, out FaceDirection direction)
    {
        if (player.BufferedDirection is { } buffered)
        {
            player.BufferedDirection = null;
            direction = buffered;
            return true;
        }

        if (_move.Value.ToFaceDirection() is { } held)
        {
            direction = held;
            return true;
        }

        direction = default;
        return false;
    }

    private bool TryReadPressedDirection(out FaceDirection direction)
    {
        if (_move.PressedLeft)
        {
            direction = FaceDirection.Left;
            return true;
        }

        if (_move.PressedRight)
        {
            direction = FaceDirection.Right;
            return true;
        }

        if (_move.PressedUp)
        {
            direction = FaceDirection.Up;
            return true;
        }

        if (_move.PressedDown)
        {
            direction = FaceDirection.Down;
            return true;
        }

        direction = default;
        return false;
    }

    public override void Shutdown()
    {
        _inputDevice.Dispose();
        _moveSprite.Dispose();
        _idleSprite.Dispose();
    }
}