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
}

public sealed class PlayerSystem : GameSystem
{
    private Query _player = null!;

    private VirtualDevice _inputDevice = null!;

    private VirtualStick _move = null!;

    private Sprite _idleSprite = null!;

    private Sprite _moveSprite = null!;

    public override void Startup()
    {
        _idleSprite = Assets.Load<Sprite>("Player/Idle");
        _moveSprite = Assets.Load<Sprite>("Player/Move");

        var player = Registry.Create();
        Registry.Set(player, new Player());
        Registry.Set(player, new SpriteRenderer { Position = Vector2.Zero, PixelSize = 2 });
        Registry.Set(player, new AnimatedSprite());

        _inputDevice = new VirtualDevice(App.Input, "Player");
        _inputDevice.IndexMode = VirtualDevice.IndexModes.AutomaticLatest;
        _move = _inputDevice.AddStick("Move",
            new StickBindingSet()
                .AddArrowKeys());

        _player = Registry.Query()
            .Include<Player>()
            .Include<SpriteRenderer>()
            .Include<AnimatedSprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        var entity = _player.Single();
        ref var player = ref Registry.Get<Player>(entity);
        ref var sprite = ref Registry.Get<SpriteRenderer>(entity);
        ref var animated = ref Registry.Get<AnimatedSprite>(entity);

        switch (player.State)
        {
            case PlayerState.Idle:
                UpdateIdle(ref player, ref sprite, ref animated);
                break;

            case PlayerState.Move:
                UpdateMove(ref player, ref sprite, ref animated, time);
                break;
        }
    }

    private void UpdateIdle(ref Player player, ref SpriteRenderer sprite, ref AnimatedSprite animated)
    {
        if (_move.Value != Vector2.Zero)
        {
            player.State = PlayerState.Move;
            return;
        }

        sprite.Sprite = _idleSprite;
        sprite.FlipHorizontal = player.Direction.IsAnimationFlipped();
        animated.AnimationIndex = _idleSprite.GetAnimationIndex(player.Direction.GetAnimationName());
        animated.Loop = true;
    }

    private void UpdateMove(ref Player player, ref SpriteRenderer sprite, ref AnimatedSprite animated, Time time)
    {
        if (_move.Value == Vector2.Zero)
        {
            player.State = PlayerState.Idle;
            return;
        }

        player.Direction = _move.Value.ToFaceDirection();
        sprite.Sprite = _moveSprite;
        sprite.Position += player.Direction.ToVector2() * 100f * time.Delta;
        sprite.FlipHorizontal = player.Direction.IsAnimationFlipped();

        animated.AnimationIndex = _moveSprite.GetAnimationIndex(player.Direction.GetAnimationName());
        animated.Loop = true;
    }

    public override void Shutdown()
    {
        _inputDevice.Dispose();
        _moveSprite.Dispose();
        _idleSprite.Dispose();
    }
}