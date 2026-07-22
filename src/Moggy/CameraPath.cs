using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct CameraPath
{
    public Vector2 From;
    public Vector2 To;
    public TimeSpan Duration;
    public TimeSpan Elapsed;
}

public sealed class CameraPathSystem : GameSystem, IGameSystemGroupState
{
    private Query _camera = null!;

    public override void Startup()
    {
        _camera = Registry.Query()
            .Include<Camera>()
            .Include<CameraPath>()
            .Include<Viewport>()
            .Build();
    }

    public override void Update(Time time)
    {
        if (!_camera.Any())
        {
            return;
        }

        var cameraEntity = _camera.Single();
        ref var camera = ref Registry.Get<Camera>(cameraEntity);
        ref var path = ref Registry.Get<CameraPath>(cameraEntity);
        ref var viewport = ref Registry.Get<Viewport>(cameraEntity);

        path.Elapsed += TimeSpan.FromSeconds(time.Delta);
        var progress = path.Duration > TimeSpan.Zero
            ? (float)Math.Clamp(path.Elapsed / path.Duration, 0f, 1f)
            : 1f;

        camera.Position = Vector2.Lerp(path.From, path.To, progress);
        ClampToLevelBounds(ref camera, in viewport);

        if (progress >= 1f)
        {
            Registry.Remove<CameraPath>(cameraEntity);
        }
    }

    public void Exit()
    {
        if (_camera.Any())
        {
            Registry.Remove<CameraPath>(_camera.Single());
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

    private static float ClampAxis(float position, float halfVisible, float halfLevel)
    {
        return halfVisible >= halfLevel
            ? 0f
            : Math.Clamp(position, -halfLevel + halfVisible, halfLevel - halfVisible);
    }
}
