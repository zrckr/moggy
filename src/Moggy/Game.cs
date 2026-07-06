using System.Numerics;
using Foster.Framework;
using ImGuiNET;

namespace Moggy;

class Game : App
{
    private readonly ImGuiRenderer _imRenderer;

    public static void Main()
    {
        Logging.Initialize();
        try
        {
            using var game = new Game();
            game.Run();
        }
        catch (Exception exception)
        {
            Serilog.Log.Fatal(exception, "Application terminated unexpectedly");
        }
        finally
        {
            Log.SetCallbacks(null, null, null);
            Serilog.Log.CloseAndFlush();
        }
    }

    private Game() : base(new AppConfig
    {
        ApplicationName = "Moggy",
        WindowTitle = "Moggy",
        Width = 1280,
        Height = 720,
        Resizable = true
    })
    {
        _imRenderer = new ImGuiRenderer(this);
    }

    protected override void Startup()
    {
    }

    protected override void Update()
    {
        _imRenderer.BeginLayout();

        // toggle text input if ImGui wants it
        if (_imRenderer.WantsTextInput)
        {
            Window.StartTextInput();
        }
        else
        {
            Window.StopTextInput();
        }

        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Appearing);
        if (ImGui.Begin("Hello Foster x Dear ImGui"))
        {
            // custom sprite batcher inside imgui window
            ImGui.Text("Some Foster Sprite Batching:");
            var size = new Vector2(ImGui.GetContentRegionAvail().X, 200);
            if (_imRenderer.BeginBatch(size, out var batch, out var bounds))
            {
                batch.CheckeredPattern(bounds, 16, 16, Color.DarkGray, Color.Gray);
                batch.Circle(bounds.Center, 32, 16, Color.Red);
            }

            _imRenderer.EndBatch();
            ImGui.Text("That was pretty cool!");
        }

        ImGui.End();
        ImGui.ShowDemoWindow();

        _imRenderer.EndLayout();
    }

    protected override void Render()
    {
        Window.Clear(Color.Black);
        _imRenderer.Render();
    }

    protected override void Shutdown()
    {
        _imRenderer.Dispose();
    }
}