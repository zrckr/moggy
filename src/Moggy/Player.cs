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

    public float MovementSpeed = 0f;

    public FaceDirection? BufferedDirection = null;
}

public sealed class PlayerSystem : GameSystem, IGameSystemGroupState
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
        _moveStick = _inputDevice.AddStick("Move", new StickBindingSet().AddWasd().AddArrowKeys());
    }

    public void Enter()
    {
        ref var level = ref Registry.Singleton<Level>();
        _playerEntity = Registry.Create(
            new Abilities(),
            new Player
            {
                MovementSpeed = _properties.MovementSpeed
            },
            new Piece(new Cell(level.Columns / 2, level.Rows / 2)), // TODO: Magezen: Player spawn
            new Sprite
            {
                Asset = _idleSprite,
                Transform = new Transform { Scale = new Vector2(_properties.Scale) },
                Animation = new SpriteAnimation(FaceDirection.Down.GetAnimationName())
            }
        );

        Registry.SetTag<Normal>(_playerEntity);
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var player = ref Registry.Get<Player>(_playerEntity);
        ref var piece = ref Registry.Get<Piece>(_playerEntity);
        ref var sprite = ref Registry.Get<Sprite>(_playerEntity);

        if (Registry.Has<PieceMove>(_playerEntity))
        {
            player.BufferedDirection = _moveStick.ToFaceDirection();
            player.State = PlayerState.Move;
        }
        else
        {
            player.State = PlayerState.Idle;

            var destination = ChooseDestinationPosition(ref player, piece, level);
            if (destination.HasValue)
            {
                player.State = PlayerState.Move;
                Registry.Set(_playerEntity, new PieceMove(piece.Position, destination.Value, player.MovementSpeed));
            }
        }

        sprite.Asset = player.State switch
        {
            PlayerState.Idle => _idleSprite,
            PlayerState.Move => _moveSprite,
            _ => throw new InvalidOperationException()
        };

        sprite.Animation.SetName(piece.FacingDirection.GetAnimationName());
        sprite.FlipH = piece.FacingDirection.IsAnimationFlipped();
    }

    private Cell? ChooseDestinationPosition(ref Player player, in Piece piece, in Level level)
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
            return null;
        }

        var direction = requested.Value.ToPoint2();
        var destination = piece.Position;
        destination.Column += direction.X;
        destination.Row += direction.Y;
        return !level.IsWalkable(destination) ? null : destination;
    }

    public void Exit()
    {
        if (Registry.Destroy(_playerEntity))
        {
            _playerEntity = Entity.Invalid;
        }
    }

    public override void Shutdown()
    {
        _inputDevice.Dispose();
        _moveSprite.Dispose();
        _idleSprite.Dispose();
    }
}
