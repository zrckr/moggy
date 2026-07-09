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

    private VirtualDevice _device = null!;

    private VirtualStick _move = null!;

    private AssetId _idleAsset;

    private AssetId _moveAsset;

    private AssetId _hurtAsset;

    public override void Startup()
    {
        _idleAsset = Assets.Load<Sprite>("Player/Idle", out _);
        _moveAsset = Assets.Load<Sprite>("Player/Move", out _);
        _hurtAsset = Assets.Load<Sprite>("Player/Hurt", out _);

        var player = Registry.Create();
        Registry.Set(player, new Player());
        Registry.Set(player, new SpriteRenderer { Position = Vector2.Zero, PixelSize = 2 });
        Registry.Set(player, new AnimatedSprite());

        _device = new VirtualDevice(App.Input, "Player");
        _device.IndexMode = VirtualDevice.IndexModes.AutomaticLatest;
        _move = _device.AddStick("Move",
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

        sprite.SpriteId = _idleAsset;
        sprite.FlipHorizontal = player.Direction.IsAnimationFlipped();

        var idleAsset = Assets.Get<Sprite>(_idleAsset);
        animated.AnimationIndex = idleAsset.GetAnimationIndex(player.Direction.GetAnimationName());
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
        sprite.SpriteId = _moveAsset;
        sprite.Position += player.Direction.ToVector2() * 100f * time.Delta;
        sprite.FlipHorizontal = player.Direction.IsAnimationFlipped();

        var moveAsset = Assets.Get<Sprite>(_moveAsset);
        animated.AnimationIndex = moveAsset.GetAnimationIndex(player.Direction.GetAnimationName());
        animated.Loop = true;
    }

    public override void Shutdown()
    {
        _device.Dispose();
        Assets.Unload(_hurtAsset);
        Assets.Unload(_moveAsset);
        Assets.Unload(_idleAsset);
    }
}