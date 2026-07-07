using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

class Game : App
{
    private readonly List<GameSystem> _systems = new();

    private readonly Registry _registry = new();

    private Batcher _batcher = null!;

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
    }

    protected override void Startup()
    {
        _batcher = new Batcher(GraphicsDevice);
    }

    protected override void Update()
    {
        foreach (var system in _systems)
        {
            system.Update(Time);
        }
    }

    protected override void Render()
    {
        Window.Clear(Color.Black);
        foreach (var system in _systems)
        {
            system.Render(Time);
        }

        _batcher.Render(Window);
        _batcher.Clear();
    }

    protected override void Shutdown()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }

        _systems.Clear();
    }

    private void AddSystem<TSystem>() where TSystem : GameSystem, new()
    {
        _systems.Add(new TSystem
        {
            Registry = _registry,
            GraphicsDevice = GraphicsDevice,
            Window = Window
        });
    }
}