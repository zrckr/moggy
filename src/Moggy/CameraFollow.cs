using Foster.Framework;
using Moggy.Ecs;
using Serilog;

namespace Moggy;

public struct CameraFollow()
{
    public Entity Target = Entity.Invalid;
    public Rect Drag = Rect.Centered(0f, 0f);
    public Rect Limits = Rect.Identity;
}

public sealed class CameraFollowSystem : GameSystem, IGameSystemGroupState
{
    private const float Half = 0.5f;

    private static readonly ILogger Logger = Serilog.Log.ForContext<CameraFollowSystem>();

    private static readonly Rect DefaultDrag = Rect.Centered(96f, 80f);

    private Query _camera = null!;

    public override void Startup()
    {
        _camera = Registry.Query()
            .Include<Camera>()
            .Include<CameraFollow>()
            .Build();
    }

    public void Enter()
    {
        ref var level = ref Registry.Singleton<Level>();
        var player = Registry.Query().Include<Player>().Build().Single();
        var camera = Registry.Query().Include<Camera>().Build().Single();

        Registry.Set(camera, new CameraFollow
        {
            Target = player,
            Drag = DefaultDrag,
            Limits = Rect.Centered(level.Width, level.Height)
        });
    }

    public override void Update(Time time)
    {
        if (!_camera.Any())
        {
            return;
        }

        var cameraEntity = _camera.Single();
        ref var follow = ref Registry.Get<CameraFollow>(cameraEntity);
        if (follow.Target == Entity.Invalid || !Registry.Has<Sprite>(follow.Target))
        {
            Logger.Warning("No target with sprite found for camera to follow.");
            return;
        }

        ref var target = ref Registry.Get<Sprite>(follow.Target);
        ref var camera = ref Registry.Get<Camera>(cameraEntity);
        var position = camera.Position;

        // Move the local drag box into world space
        var drawWorld = follow.Drag.Translate(camera.Position);

        // Move camera only when target leaves the drag box
        position += drawWorld.Difference(target.Transform.Position);

        // Keep the camera's world-space bounds inside the follow limits
        var cameraBounds = camera.Bounds.Translate(position - camera.Position);
        position -= follow.Limits.Difference(cameraBounds);
        camera.Position = position;
    }

    public void Exit()
    {
        Registry.Remove<CameraFollow>(_camera.Single());
    }
}
