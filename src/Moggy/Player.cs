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

    private Query _level = null!;

    private Query _player = null!;

    private VirtualDevice _inputDevice = null!;

    private VirtualStick _move = null!;

    private SpriteAsset _idleSprite = null!;

    private SpriteAsset _moveSprite = null!;

    public override void Startup()
    {
        _level = Registry.Query()
            .Include<Level>()
            .Build();

        _idleSprite = Assets.Load<SpriteAsset>("Player/Idle");
        _moveSprite = Assets.Load<SpriteAsset>("Player/Move");

        var levelEntity = _level.Single();
        ref var level = ref Registry.Get<Level>(levelEntity);
        var startCell = level.FindNearestWalkableCell(new Point2(level.Columns / 2, level.Rows / 2));

        var player = Registry.Create();
        Registry.Set(player, new Player());
        Registry.Set(player, new LevelPosition
        {
            Cell = startCell
        });
        Registry.Set(player, new Transform
        {
            Position = level.CellToCenter(startCell),
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
            .Include<LevelPosition>()
            .Include<Transform>()
            .Include<AnimatedSprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        var playerEntity = _player.Single();
        var levelEntity = _level.Single();
        ref var level = ref Registry.Get<Level>(levelEntity);
        ref var player = ref Registry.Get<Player>(playerEntity);
        ref var levelPosition = ref Registry.Get<LevelPosition>(playerEntity);
        ref var animated = ref Registry.Get<AnimatedSprite>(playerEntity);

        if (Registry.Has<LevelMover>(playerEntity))
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

            if (TryChooseMoveDirection(ref player, in level, in levelPosition, out var direction) &&
                TryStartMove(playerEntity, in level, in levelPosition, direction))
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

    private bool TryStartMove(Entity entity, in Level level, in LevelPosition levelPosition, FaceDirection direction)
    {
        var target = levelPosition.Cell + direction.ToPoint2();
        if (!level.IsWalkable(target))
        {
            return false;
        }

        Registry.Set(entity, new LevelMover
        {
            From = levelPosition.Cell,
            To = target,
            Progress = 0f,
            Speed = 1f / CellMoveDuration
        });

        return true;
    }

    private bool TryChooseMoveDirection(
        ref Player player,
        in Level level,
        in LevelPosition levelPosition,
        out FaceDirection direction)
    {
        if (!TryReadBufferedOrHeldDirection(ref player, out var requested))
        {
            direction = default;
            return false;
        }

        player.Direction = requested;

        if (!level.IsWalkable(levelPosition.Cell + requested.ToPoint2()))
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