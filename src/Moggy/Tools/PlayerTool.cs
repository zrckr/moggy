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
            .Include<SpriteTransform>()
            .Include<AnimatedSprite>()
            .Build();
    }

    public override void Draw(Time time)
    {
        var entity = _player.Single();
        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ref var player = ref Registry.Get<Player>(entity);
            ref var levelTransform = ref Registry.Get<LevelTransform>(entity);
            ref var spriteTransform = ref Registry.Get<SpriteTransform>(entity);
            ref var sprite = ref Registry.Get<AnimatedSprite>(entity);

            ImGui.LabelText("Entity", entity.Id.ToString());
            ImGui.LabelText("State", player.State.ToString());
            ImGui.LabelText("Previous", player.PreviousState.ToString());
            ImGui.LabelText("Direction", player.Direction.ToString());
            ImGui.LabelText("Buffered", player.BufferedDirection?.ToString() ?? "-");

            ImGui.SeparatorText("Movement");
            ImGui.LabelText("Cell", levelTransform.Position.ToString());
            ImGui.LabelText("Position", spriteTransform.Position.ToString());
            ImGui.LabelText("Scale", spriteTransform.Scale.ToString());
            ImGui.LabelText("Moving", Registry.Has<LevelMover>(entity).ToString());

            ImGui.SeparatorText("Sprite");
            ImGui.LabelText("Animation", sprite.Animation);
            ImGui.LabelText("Frame", sprite.Frame.ToString());
            ImGui.LabelText("Flip H", sprite.FlipHorizontal.ToString());
        }

        ImGui.End();
    }
}