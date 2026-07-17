using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct Camera(Vector2 position)
{
    public Vector2 Position = position;

    public float Zoom = 1f;

    public Matrix3x2 WorldToVirtual = Matrix3x2.Identity;

    public Matrix3x2 VirtualToWorld = Matrix3x2.Identity;
}

public sealed class CameraSystem : GameSystem
{
    public override void Render(Time time)
    {
        ref var camera = ref Registry.Singleton<Camera>();
        ref var viewport = ref Registry.Singleton<Viewport>();

        camera.WorldToVirtual =
            Matrix3x2.CreateTranslation(-camera.Position) *
            Matrix3x2.CreateScale(camera.Zoom) *
            Matrix3x2.CreateTranslation(viewport.ContentBounds.CenterF);

        Matrix3x2.Invert(camera.WorldToVirtual, out var inverse);
        camera.VirtualToWorld = inverse;
    }
}
