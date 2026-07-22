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

public sealed class PlayerGameSystem : GameSystem, IGameSystemGroupState
{
    private VirtualDevice _inputDevice = null!;

    private VirtualStick _moveStick = null!;

    private SpriteAsset _idleSprite = null!;

    private SpriteAsset _moveSprite = null!;

    private AbilityProperties _properties = null!;

    private Entity _playerEntity = Entity.Invalid;

    public override void Startup()
    {
        _properties = Assets.LoadJson<AbilityProperties>("Player/Normal");
        _idleSprite = Assets.Load<SpriteAsset>(_properties.IdleSprite);
        _moveSprite = Assets.Load<SpriteAsset>(_properties.MoveSprite);

        _inputDevice = new VirtualDevice(Game.Input, "Player");
        _inputDevice.IndexMode = VirtualDevice.IndexModes.AutomaticLatest;
        _moveStick = _inputDevice.AddStick("Move",
            new StickBindingSet()
                .AddWasd()
                .AddArrowKeys());
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var player = ref Registry.Get<Player>(_playerEntity);
        ref var transform = ref Registry.Get<LevelTransform>(_playerEntity);
        ref var sprite = ref Registry.Get<Sprite>(_playerEntity);

        if (Registry.Has<LevelMover>(_playerEntity))
        {
            player.BufferedDirection = _moveStick.ToFaceDirection();
            player.State = PlayerState.Move;
            sprite.Asset = _moveSprite;
        }
        else
        {
            player.State = PlayerState.Idle;

            if (TryChooseMoveDirection(ref player, in level, in transform, out var direction) &&
                TryStartMove(_playerEntity, in level, in transform, direction))
            {
                player.State = PlayerState.Move;
                sprite.Asset = _moveSprite;
            }
            else
            {
                sprite.Asset = _idleSprite;
            }
        }

        sprite.Animation.SetName(player.Direction.GetAnimationName());
        sprite.FlipH = player.Direction.IsAnimationFlipped();
    }

    private bool TryStartMove(Entity entity, in Level level, in LevelTransform levelTransform, FaceDirection direction)
    {
        if (!level.TryResolveMove(levelTransform.Position, direction, out var move))
        {
            return false;
        }

        Registry.Set(entity, new LevelMover
        {
            From = levelTransform.Position,
            To = move.To,
            VisualWrapTo = move.VisualWrapTo,
            Progress = 0f,
            Speed = _properties.MovementSpeed
        });

        return true;
    }

    private bool TryChooseMoveDirection(
        ref Player player,
        in Level level,
        in LevelTransform levelTransform,
        out FaceDirection direction)
    {
        FaceDirection? requested = null;
        if (player.BufferedDirection is { } buffered)
        {
            player.BufferedDirection = null;
            requested = buffered;
        }
        else if (_moveStick.Value.ToFaceDirection() is { } held)
        {
            requested = held;
        }

        if (!requested.HasValue)
        {
            direction = default;
            return false;
        }

        player.Direction = requested.Value;
        if (!level.TryResolveMove(levelTransform.Position, requested.Value, out _))
        {
            direction = default;
            return false;
        }

        direction = requested.Value;
        return true;
    }

    public override void Shutdown()
    {
        _inputDevice.Dispose();
        _moveSprite.Dispose();
        _idleSprite.Dispose();
    }

    public void Enter()
    {
        ref var level = ref Registry.Singleton<Level>();
        var startCell = level.FindNearestWalkableCell(new Cell(level.Columns / 2, level.Rows / 2));

        _playerEntity = Registry.Create(
            new Player(),
            new LevelTransform
            {
                Position = startCell
            },
            new Sprite
            {
                Asset = _idleSprite,
                Transform = new Transform(level.CellToCenter(startCell), new Vector2(_properties.Scale), 0f),
                Animation = new SpriteAnimation(FaceDirection.Down.GetAnimationName())
            },
            new Abilities());

        Registry.SetTag<NavigationTarget>(_playerEntity);
        Registry.SetTag<Normal>(_playerEntity);
    }

    public void Exit()
    {
        Registry.Destroy(_playerEntity);
        _playerEntity = Entity.Invalid;
    }
}
