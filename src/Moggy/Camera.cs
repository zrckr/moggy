using System.Numerics;
using Foster.Framework;

namespace Moggy;

public struct Camera()
{
    public Vector2 Position = Vector2.Zero;
    public float Zoom = 1f;
    public Vector2 ViewportSize = Vector2.Zero;
    public Matrix3x2 WorldToVirtual = Matrix3x2.Identity;
    public Matrix3x2 VirtualToWorld = Matrix3x2.Identity;
    public Rect Bounds => Rect.Centered(Position, ViewportSize / Zoom);
}

public sealed class CameraSystem : GameSystem
{
    public override void Render(Time time)
    {
        ref var camera = ref Registry.Singleton<Camera>();
        ref var viewport = ref Registry.Singleton<Viewport>();

        camera.ViewportSize = new Vector2(viewport.ContentBounds.Width, viewport.ContentBounds.Height);
        camera.WorldToVirtual =
            Matrix3x2.CreateTranslation(-camera.Position.Round()) *
            Matrix3x2.CreateScale(camera.Zoom) *
            Matrix3x2.CreateTranslation(viewport.ContentBounds.CenterF);

        Matrix3x2.Invert(camera.WorldToVirtual, out var inverse);
        camera.VirtualToWorld = inverse;
    }
}
