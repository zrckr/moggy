using System.Numerics;
using Foster.Framework;
using ImGuiNET;

namespace Moggy.Tools;

public sealed class PerformanceTool : ToolSystem
{
    private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize |
                                                 ImGuiWindowFlags.NoCollapse |
                                                 ImGuiWindowFlags.NoFocusOnAppearing |
                                                 ImGuiWindowFlags.NoNav |
                                                 ImGuiWindowFlags.NoSavedSettings;

    private const float MinimumDelta = 0.0001f;

    private const float MillisecondsPerSecond = 1000f;

    private const float SmoothingFactor = 0.1f;

    public override string Title => "Performance";

    private float _averageDelta;

    public PerformanceTool()
    {
        IsOpen = true;
    }

    public override void Draw(Time time)
    {
        if (_averageDelta <= 0f)
        {
            _averageDelta = time.Delta;
        }
        else
        {
            _averageDelta += (time.Delta - _averageDelta) * SmoothingFactor;
        }

        ImGui.SetNextWindowPos(new Vector2(8f, 28f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(Title, ref IsOpen, WindowFlags))
        {
            var delta = Math.Max(time.Delta, MinimumDelta);
            var averageDelta = Math.Max(_averageDelta, MinimumDelta);
            var frameTime = (delta * MillisecondsPerSecond).ToString("0.00") + " ms";

            ImGui.LabelText("FPS", (1f / averageDelta).ToString("0"));
            ImGui.LabelText("Frame", frameTime);
        }

        ImGui.End();
    }
}
