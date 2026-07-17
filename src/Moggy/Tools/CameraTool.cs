using System.Numerics;
using Foster.Framework;
using ImGuiNET;
using Moggy.Ecs;

namespace Moggy.Tools;

public sealed class CameraTool : ToolSystem
{
    public override string Title => "Camera";

    private Query _camera = null!;

    private bool _showDragRect = true;

    public override void Startup()
    {
        _camera = Registry.Query()
            .Include<Camera>()
            .Include<Viewport>()
            .Include<CameraFollow>()
            .Build();
    }

    public override void Draw(Time time)
    {
        var entity = _camera.Single();
        ref var camera = ref Registry.Get<Camera>(entity);
        ref var viewport = ref Registry.Get<Viewport>(entity);
        ref var follow = ref Registry.Get<CameraFollow>(entity);

        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.LabelText("Entity", entity.Id.ToString());
            ImGui.LabelText("Position", camera.Position.ToString());

            ImGui.SeparatorText("Dragging");
            ImGui.DragFloat2("Size", ref follow.DragSize);
            ImGui.SameLine();
            ImGui.Checkbox("Overlay?", ref _showDragRect);
        }

        ImGui.End();

        if (_showDragRect)
        {
            var center = viewport.ContentBounds.CenterF;
            var min = center - (follow.DragSize * 0.5f);
            var max = center + (follow.DragSize * 0.5f);
            var screenOffset = new Vector2(viewport.WindowBounds.X, viewport.WindowBounds.Y);
            var screenMin = screenOffset + (min * viewport.Scale);
            var screenMax = screenOffset + (max * viewport.Scale);
            ImGui.GetForegroundDrawList().AddRect(screenMin, screenMax, Color.Blue.ABGR);
        }
    }
}
