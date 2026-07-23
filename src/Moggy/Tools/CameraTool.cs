using System.Numerics;
using Foster.Framework;
using ImGuiNET;
using Moggy.Ecs;

namespace Moggy.Tools;

public sealed class CameraTool : ToolSystem
{
    private static readonly Color DragRectColor = Color.Blue;

    private static readonly Color LimitsRectColor = Color.Red;

    public override string Title => "Camera";

    private Query _camera = null!;

    private bool _showDragRect = true;

    private bool _showLimitsRect = true;

    public override void Startup()
    {
        _camera = Registry.Query<Camera, Viewport, CameraFollow>();
    }

    public override void Draw(Time time)
    {
        if (!_camera.Any())
        {
            if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Camera follow inactive");
            }

            ImGui.End();
            return;
        }

        var entity = _camera.Single();
        ref var camera = ref Registry.Get<Camera>(entity);
        ref var viewport = ref Registry.Get<Viewport>(entity);
        ref var follow = ref Registry.Get<CameraFollow>(entity);

        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.LabelText("Position", camera.Position.ToString());
            ImGui.SeparatorText("Dragging");

            var dragPosition = follow.Drag.Position;
            if (ImGui.DragFloat2("Position", ref dragPosition))
            {
                follow.Drag.Position = dragPosition;
            }

            var dragSize = follow.Drag.Size;
            if (ImGui.DragFloat2("Size", ref dragSize))
            {
                follow.Drag.Size = dragSize;
            }

            ImGui.SameLine();
            ImGui.Checkbox("Drag overlay", ref _showDragRect);

            ImGui.SeparatorText("Limits");
            ImGui.LabelText("Position##Limits", follow.Limits.Position.ToString());
            ImGui.LabelText("Size##Limits", follow.Limits.Size.ToString());
            ImGui.Checkbox("Limits overlay", ref _showLimitsRect);
        }

        ImGui.End();

        if (_showDragRect)
        {
            // Match the world-space drag rectangle used by CameraFollowSystem
            var dragWorld = follow.Drag.Translate(camera.Position);
            DrawWorldRect(dragWorld, DragRectColor, camera, viewport);
        }

        if (_showLimitsRect)
        {
            // Limits constrain the camera position and already use world coordinates
            DrawWorldRect(follow.Limits, LimitsRectColor, camera, viewport);
        }
    }

    private static void DrawWorldRect(in Rect rect, in Color color, in Camera camera, in Viewport viewport)
    {
        var screenMin = WorldToScreen(rect.TopLeft, camera, viewport);
        var screenMax = WorldToScreen(rect.BottomRight, camera, viewport);
        ImGui.GetForegroundDrawList().AddRect(screenMin, screenMax, color.ABGR);
    }

    private static Vector2 WorldToScreen(in Vector2 position, in Camera camera, in Viewport viewport)
    {
        var virtualPosition = Vector2.Transform(position, camera.WorldToVirtual);
        var screenOffset = new Vector2(viewport.WindowBounds.X, viewport.WindowBounds.Y);
        return screenOffset + (virtualPosition * viewport.Scale);
    }
}
