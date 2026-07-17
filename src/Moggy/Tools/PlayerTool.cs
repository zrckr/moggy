using Foster.Framework;
using ImGuiNET;
using Moggy.Ecs;

namespace Moggy.Tools;

public sealed class PlayerTool : ToolSystem
{
    public override string Title => "Player";

    private Query _player = null!;

    public override void Startup()
    {
        _player = Registry.Query()
            .Include<Player>()
            .Include<LevelTransform>()
            .Include<Sprite>()
            .Build();
    }

    public override void Draw(Time time)
    {
        var entity = _player.Single();
        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ref var player = ref Registry.Get<Player>(entity);
            ref var levelTransform = ref Registry.Get<LevelTransform>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            ImGui.LabelText("Entity", entity.Id.ToString());
            ImGui.LabelText("State", player.State.ToString());
            ImGui.LabelText("Previous", player.PreviousState.ToString());
            ImGui.LabelText("Direction", player.Direction.ToString());
            ImGui.LabelText("Buffered", player.BufferedDirection?.ToString() ?? "-");

            ImGui.SeparatorText("Movement");
            ImGui.LabelText("Cell", levelTransform.Position.ToString());
            ImGui.LabelText("Position", sprite.Transform.Position.ToString());
            ImGui.LabelText("Scale", sprite.Transform.Scale.ToString());
            ImGui.LabelText("Moving", Registry.Has<LevelMover>(entity).ToString());

            if (sprite.Animation is { } animation)
            {
                ImGui.SeparatorText("Animation");
                ImGui.LabelText("Animation", animation.Name);
                ImGui.LabelText("Frame", animation.Frame.ToString());
            }
        }

        ImGui.End();
    }
}
