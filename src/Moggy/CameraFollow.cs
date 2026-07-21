using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct CameraFollow
{
    public Entity Target;

    public Vector2 DragSize;
}

public sealed class CameraFollowSystem : GameSystem, ILevelParticipant
{
    private static readonly Vector2 DefaultDragSize = new(96f, 80f);

    private Query _camera = null!;

    private Query _player = null!;

    public override void Startup()
    {
        _camera = Registry.Query()
            .Include<Camera>()
            .Include<Viewport>()
            .Build();

        _player = Registry.Query()
            .Include<Player>()
            .Include<Sprite>()
            .Build();

    }

    public override void Update(Time time)
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        if (game.State != GameState.Level)
        {
            return;
        }

        var cameraEntity = _camera.Single();
        EnsureFollowTarget(cameraEntity);

        ref var camera = ref Registry.Get<Camera>(cameraEntity);
        ref var follow = ref Registry.Get<CameraFollow>(cameraEntity);

        var halfDrag = follow.DragSize / (2f * camera.Zoom);
        var min = camera.Position - halfDrag;
        var max = camera.Position + halfDrag;

        ref var sprite = ref Registry.Get<Sprite>(follow.Target);
        var targetPosition = sprite.Transform.Position;
        if (targetPosition.X < min.X)
        {
            camera.Position.X = targetPosition.X + halfDrag.X;
        }
        else if (targetPosition.X > max.X)
        {
            camera.Position.X = targetPosition.X - halfDrag.X;
        }

        if (targetPosition.Y < min.Y)
        {
            camera.Position.Y = targetPosition.Y + halfDrag.Y;
        }
        else if (targetPosition.Y > max.Y)
        {
            camera.Position.Y = targetPosition.Y - halfDrag.Y;
        }

        ref var viewport = ref Registry.Get<Viewport>(cameraEntity);
        ClampToLevelBounds(ref camera, in viewport);
    }

    public void EnterLevel(LevelStartMode mode)
    {
        var camera = _camera.Single();
        var target = _player.Single();
        if (Registry.Has<CameraFollow>(camera))
        {
            ref var follow = ref Registry.Get<CameraFollow>(camera);
            follow.Target = target;
        }
        else
        {
            Registry.Set(camera, new CameraFollow
            {
                Target = target,
                DragSize = DefaultDragSize
            });
        }
    }

    public void ExitLevel()
    {
        var camera = _camera.Single();
        Registry.Remove<CameraFollow>(camera);
    }

    private void EnsureFollowTarget(Entity camera)
    {
        ref var follow = ref Registry.Get<CameraFollow>(camera);
        if (!Registry.IsAlive(follow.Target))
        {
            follow.Target = _player.Single();
        }
    }

    private void ClampToLevelBounds(ref Camera camera, in Viewport viewport)
    {
        ref var level = ref Registry.Singleton<Level>();
        var halfVisible = new Vector2(viewport.ContentBounds.Width, viewport.ContentBounds.Height) / (2f * camera.Zoom);
        var halfLevel = new Vector2(level.Width, level.Height) * 0.5f;

        camera.Position.X = ClampAxis(camera.Position.X, halfVisible.X, halfLevel.X);
        camera.Position.Y = ClampAxis(camera.Position.Y, halfVisible.Y, halfLevel.Y);
    }

    private static float ClampAxis(float position, float halfVisible, float halfGrid)
    {
        return halfVisible >= halfGrid
            ? 0f
            : Math.Clamp(position, -halfGrid + halfVisible, halfGrid - halfVisible);
    }
}
