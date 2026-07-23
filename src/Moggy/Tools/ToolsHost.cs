using Foster.Framework;
using ImGuiNET;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy.Tools;

public sealed class ToolsHost
{
    private readonly App _app;

    private readonly Registry _registry;

    private readonly AssetLoader _assets;

    private readonly List<ToolSystem> _tools = new();

    public ToolsHost(App app, Registry registry, AssetLoader assets)
    {
        _app = app;
        _registry = registry;
        _assets = assets;
        RegisterTool<PerformanceTool>();
        RegisterTool<PlayerTool>();
        RegisterTool<CameraTool>();
        RegisterTool<GridTool>();
        RegisterTool<LevelTilesTool>();
    }

    public void Draw(Time time)
    {
        var menuBarHeight = ImGui.GetFrameHeight();
        var mousePosition = ImGui.GetMousePos();
        var displaySize = ImGui.GetIO().DisplaySize;

        // Use the menu height as an activation band without reserving viewport space.
        var pointerInMenuBarRegion = mousePosition.X >= 0f &&
                                     mousePosition.X < displaySize.X &&
                                     mousePosition.Y >= 0f &&
                                     mousePosition.Y < menuBarHeight;
        var menuOpen = ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId);
        var menuBarVisible = pointerInMenuBarRegion || menuOpen;

        if (menuBarVisible && ImGui.BeginMainMenuBar())
        {
            foreach (var tool in _tools)
            {
                ImGui.MenuItem(tool.Title, "", ref tool.IsOpen);
            }

            ImGui.EndMainMenuBar();
        }

        foreach (var tool in _tools)
        {
            if (tool.IsOpen)
            {
                tool.Draw(time);
            }
        }
    }

    private void RegisterTool<T>() where T : ToolSystem, new()
    {
        var tool = new T
        {
            App = _app,
            Registry = _registry,
            Assets = _assets
        };

        _tools.Add(tool);
        tool.Startup();
    }
}
