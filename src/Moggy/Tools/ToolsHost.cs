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
        RegisterTool<NavigationTool>();
    }

    public void Draw(Time time)
    {
        if (ImGui.BeginMainMenuBar())
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
            Assets = _assets,
        };

        _tools.Add(tool);
        tool.Startup();
    }
}
