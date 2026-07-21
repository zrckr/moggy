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

public sealed class PlayerSystem : GameSystem, ILevelParticipant
{
    private VirtualDevice _inputDevice = null!;

    private VirtualStick _moveStick = null!;

    private SpriteAsset _idleSprite = null!;

    private SpriteAsset _moveSprite = null!;

    private PlayerDefinition _definition = null!;

    private Entity _playerEntity = Entity.Invalid;

    public override void Startup()
    {
        _definition = Assets.LoadJson<PlayerDefinition>("Player/Definition");
        _idleSprite = Assets.Load<SpriteAsset>(_definition.IdleSprite);
        _moveSprite = Assets.Load<SpriteAsset>(_definition.MoveSprite);

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
        var target = levelTransform.Position + direction;
        if (!level.IsWalkable(target))
        {
            return false;
        }

        Registry.Set(entity, new LevelMover
        {
            From = levelTransform.Position,
            To = target,
            Progress = 0f,
            Speed = _definition.MovementSpeed
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
        if (!level.IsWalkable(levelTransform.Position + requested.Value))
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

    public void EnterLevel(LevelStartMode mode)
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
                Transform = new Transform(level.CellToCenter(startCell), new Vector2(2f), 0f),
                Animation = new SpriteAnimation(FaceDirection.Down.GetAnimationName())
            });

        Registry.SetTag<NavigationTarget>(_playerEntity);
    }

    public void ExitLevel()
    {
        Registry.Destroy(_playerEntity);
        _playerEntity = Entity.Invalid;
    }
}
