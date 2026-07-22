using Foster.Framework;
using ImGuiNET;

namespace Moggy.Tools;

public sealed class LevelTilesTool : ToolSystem
{
    public override string Title => "Level Tiles";

    public override void Draw(Time time)
    {
        ref var debug = ref Registry.Singleton<LevelDebug>();

        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Checkbox("Show overlay", ref debug.ShowTiles);
        }

        ImGui.End();
    }
}
