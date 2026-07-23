using System.Globalization;
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
        _player = Registry.Query<Player, Piece, Sprite>();
    }

    public override void Draw(Time time)
    {
        var entity = _player.Single();
        if (ImGui.Begin(Title, ref IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ref var player = ref Registry.Get<Player>(entity);
            ref var piece = ref Registry.Get<Piece>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            ImGui.SeparatorText("Player");
            ImGui.LabelText("State", player.State.ToString());
            ImGui.LabelText("Previous", player.PreviousState.ToString());
            ImGui.LabelText("Buffered", player.BufferedDirection?.ToString() ?? "-");
            ImGui.LabelText("Movement Speed", player.MovementSpeed.ToString(CultureInfo.InvariantCulture));

            ImGui.SeparatorText("Piece");
            ImGui.LabelText("Cell", piece.Position.ToString());
            ImGui.LabelText("Facing Direction", piece.FacingDirection.ToString());
            ImGui.LabelText("Moving", Registry.Has<PieceMove>(entity).ToString());

            ImGui.SeparatorText("Sprite");
            ImGui.LabelText("Position", sprite.Transform.Position.ToString());
            ImGui.LabelText("Scale", sprite.Transform.Scale.ToString());
            if (sprite.Animation is { } animation)
            {
                ImGui.LabelText("Animation", animation.Name);
                ImGui.LabelText("Frame", animation.Frame.ToString());
            }
        }

        ImGui.End();
    }
}
