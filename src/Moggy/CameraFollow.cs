using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct CameraFollow
{
    public Entity Target;

    public Vector2 DragSize;
}

public sealed class CameraFollowSystem : GameSystem
{
    private static readonly Vector2 DefaultDragSize = new(96f, 80f);

    private Query _camera = null!;

    private Query _level = null!;

    private Query _player = null!;

    public override void Startup()
    {
        _camera = Registry.Query()
            .Include<Camera>()
            .Include<Viewport>()
            .Build();

        _level = Registry.Query()
            .Include<Level>()
            .Build();

        _player = Registry.Query()
            .Include<Player>()
            .Include<Transform>()
            .Build();

        var camera = _camera.Single();
        if (!Registry.Has<CameraFollow>(camera))
        {
            Registry.Set(camera, new CameraFollow
            {
                Target = _player.Single(),
                DragSize = DefaultDragSize
            });
        }
    }

    public override void Update(Time time)
    {
        var cameraEntity = _camera.Single();
        EnsureFollowTarget(cameraEntity);

        ref var camera = ref Registry.Get<Camera>(cameraEntity);
        ref var viewport = ref Registry.Get<Viewport>(cameraEntity);
        ref var follow = ref Registry.Get<CameraFollow>(cameraEntity);
        ref var target = ref Registry.Get<Transform>(follow.Target);

        var halfDrag = follow.DragSize / (2f * camera.Zoom);
        var min = camera.Position - halfDrag;
        var max = camera.Position + halfDrag;

        if (target.Position.X < min.X)
        {
            camera.Position.X = target.Position.X + halfDrag.X;
        }
        else if (target.Position.X > max.X)
        {
            camera.Position.X = target.Position.X - halfDrag.X;
        }

        if (target.Position.Y < min.Y)
        {
            camera.Position.Y = target.Position.Y + halfDrag.Y;
        }
        else if (target.Position.Y > max.Y)
        {
            camera.Position.Y = target.Position.Y - halfDrag.Y;
        }

        ClampToLevelBounds(ref camera, in viewport);
    }

    private void EnsureFollowTarget(Entity camera)
    {
        ref var follow = ref Registry.Get<CameraFollow>(camera);
        if (Registry.IsAlive(follow.Target) && Registry.Has<Transform>(follow.Target))
        {
            return;
        }

        follow.Target = _player.Single();
    }

    private void ClampToLevelBounds(ref Camera camera, in Viewport viewport)
    {
        var levelEntity = _level.Single();
        ref var level = ref Registry.Get<Level>(levelEntity);

        var halfVisible = new Vector2(viewport.ContentBounds.Width, viewport.ContentBounds.Height) / (2f * camera.Zoom);
        var halfLevel = new Vector2(level.Width, level.Height) * 0.5f;

        camera.Position.X = ClampAxis(camera.Position.X, halfVisible.X, halfLevel.X);
        camera.Position.Y = ClampAxis(camera.Position.Y, halfVisible.Y, halfLevel.Y);
    }

    private static float ClampAxis(float position, float halfVisible, float halfGrid)
    {
        if (halfVisible >= halfGrid)
        {
            return 0f;
        }

        return Math.Clamp(position, -halfGrid + halfVisible, halfGrid - halfVisible);
    }
}