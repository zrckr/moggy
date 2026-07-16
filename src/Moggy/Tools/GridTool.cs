using System.Numerics;
using Foster.Framework;
using ImGuiNET;
using Moggy.Ecs;

namespace Moggy.Tools;

public class GridTool : ToolSystem
{
    private static readonly Color LineColor = Color.DarkGray;

    private const int CellSize = 8;

    private const float LineThickness = 1f;

    public override string Title => "Grid";

    public override void Draw(Time time)
    {
        ref var viewport = ref Registry.Singleton<Viewport>();

        var screenOrigin = new Vector2(viewport.WindowBounds.X, viewport.WindowBounds.Y);
        var screenWidth = viewport.VirtualWidth * viewport.Scale;
        var screenHeight = viewport.VirtualHeight * viewport.Scale;
        var drawList = ImGui.GetForegroundDrawList();

        for (var x = 0; x <= viewport.VirtualWidth; x += CellSize)
        {
            var screenX = screenOrigin.X + x * viewport.Scale;
            var start = new Vector2(screenX, screenOrigin.Y);
            var end = new Vector2(screenX, screenOrigin.Y + screenHeight);

            drawList.AddLine(start, end, LineColor.ABGR, LineThickness);
        }

        for (var y = 0; y <= viewport.VirtualHeight; y += CellSize)
        {
            var screenY = screenOrigin.Y + y * viewport.Scale;
            var start = new Vector2(screenOrigin.X, screenY);
            var end = new Vector2(screenOrigin.X + screenWidth, screenY);

            drawList.AddLine(start, end, LineColor.ABGR, LineThickness);
        }
    }
}
