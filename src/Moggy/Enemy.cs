using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public sealed record EnemyProperties
{
    public int Count { get; init; }

    public float MovementSpeed { get; init; }

    public int SpawnSeed { get; init; }

    public float Scale { get; init; } = 1f;

    public string MoveSprite { get; init; } = string.Empty;
}

public struct Enemy()
{
    public float MovementSpeed = 0f;
}

public sealed class EnemySystem : GameSystem, IGameSystemGroupState
{
    private EnemyProperties _properties = null!;

    private SpriteAsset _moveSprite = null!;

    private readonly List<Entity> _enemyEntities = new();

    public override void Startup()
    {
        _properties = Assets.LoadJson<EnemyProperties>("Enemy/Properties");
        _moveSprite = Assets.Load<SpriteAsset>(_properties.MoveSprite);
    }

    public void Enter()
    {
        ref var level = ref Registry.Singleton<Level>();
        var random = new Random(_properties.SpawnSeed);

        for (var index = 0; index < _properties.Count; index++)
        {
            var origin = new Cell(random.Next(level.Columns), random.Next(level.Rows));
            var enemy = Registry.Create(
                new Enemy
                {
                    MovementSpeed = _properties.MovementSpeed
                },
                new Piece(origin),
                new Sprite
                {
                    Asset = _moveSprite,
                    Transform = new Transform { Scale = new Vector2(_properties.Scale) },
                    Animation = new SpriteAnimation(FaceDirection.Down.GetAnimationName())
                });

            _enemyEntities.Add(enemy);
        }
    }

    public override void Update(Time time)
    {
        foreach (var entity in _enemyEntities)
        {
            ref var piece = ref Registry.Get<Piece>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            sprite.Animation.SetName(piece.FacingDirection.GetAnimationName());
            sprite.FlipH = piece.FacingDirection.IsAnimationFlipped();
        }
    }

    public void Exit()
    {
        foreach (var enemy in _enemyEntities)
        {
            Registry.Destroy(enemy);
        }

        _enemyEntities.Clear();
    }

    public override void Shutdown()
    {
        _moveSprite.Dispose();
    }
}
