using System.Numerics;
using Foster.Framework;
using ImGuiNET;
using Moggy.Ecs;

namespace Moggy.Tools;

public sealed class NavigationTool : ToolSystem
{
    private static readonly Color RaycastColor = Color.Green;

    private static readonly Color TraceColor = Color.Yellow;

    private const float LineThickness = 1.5f;

    private const float TraceMarkerSize = 4f;

    public override string Title => "Navigation";

    private Query _enemies = null!;

    private Query _target = null!;

    private bool _showRaycasts = true;

    private bool _showTrace = true;

    public override void Startup()
    {
        _enemies = Registry.Query()
            .Include<Enemy>()
            .Include<LevelPosition>()
            .Build();

        _target = Registry.Query()
            .Include<NavigationTarget>()
            .Include<LevelPosition>()
            .Build();

    }

    public override void Draw(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var navigation = ref Registry.Singleton<Navigation>();
        ref var camera = ref Registry.Singleton<Camera>();
        ref var viewport = ref Registry.Singleton<Viewport>();

        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.LabelText("Trace cells", navigation.GetTrace().Count.ToString());
            ImGui.Checkbox("Raycasts", ref _showRaycasts);
            ImGui.Checkbox("Trace", ref _showTrace);
        }

        ImGui.End();

        var drawList = ImGui.GetForegroundDrawList();
        if (_showTrace)
        {
            DrawTrace(drawList, in level, in navigation, in camera, in viewport);
        }

        if (_showRaycasts)
        {
            ref var targetPosition = ref Registry.Get<LevelPosition>(_target.Single());
            DrawRaycasts(drawList, in level, targetPosition.Cell, in camera, in viewport);
        }
    }

    private void DrawRaycasts(
        ImDrawListPtr drawList,
        in Level level,
        Cell target,
        in Camera camera,
        in Viewport viewport)
    {
        var targetPosition = ToScreen(in level, target, in camera, in viewport);

        foreach (var entity in _enemies)
        {
            ref var position = ref Registry.Get<LevelPosition>(entity);
            if (!level.TryRaycast(position.Cell, target, out _))
            {
                continue;
            }

            var enemyPosition = ToScreen(in level, position.Cell, in camera, in viewport);
            drawList.AddLine(enemyPosition, targetPosition, RaycastColor.ABGR, LineThickness);
        }
    }

    private static void DrawTrace(
        ImDrawListPtr drawList,
        in Level level,
        in Navigation navigation,
        in Camera camera,
        in Viewport viewport)
    {
        var trace = navigation.GetTrace();
        for (var index = 0; index < trace.Count; index++)
        {
            var position = ToScreen(in level, trace[index], in camera, in viewport);
            var extent = Vector2.One * (TraceMarkerSize * viewport.Scale * 0.5f);
            drawList.AddRect(position - extent, position + extent, TraceColor.ABGR);

            if (index == 0)
            {
                continue;
            }

            var previous = ToScreen(in level, trace[index - 1], in camera, in viewport);
            drawList.AddLine(previous, position, TraceColor.ABGR, LineThickness);
        }
    }

    private static Vector2 ToScreen(in Level level, Cell cell, in Camera camera, in Viewport viewport)
    {
        var virtualPosition = Vector2.Transform(level.CellToCenter(cell), camera.WorldToVirtual);
        var windowOrigin = new Vector2(viewport.WindowBounds.X, viewport.WindowBounds.Y);
        return windowOrigin + virtualPosition * viewport.Scale;
    }
}