using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public class Game : App
{
    private readonly List<GameSystem> _systems = new();

    private readonly Registry _registry = new();

    private AssetLoader _assets = null!;

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

        #region Loaders

        _assets = new AssetLoader(this);
        _assets.Register<Sprite>();

        #endregion

        #region Systems

        RegisterSystem<PlayerSystem>();
        RegisterSystem<SpriteSystem>();

        #endregion
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
        _assets.Check();
    }

    protected override void Shutdown()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }

        _systems.Clear();
        _assets.Dispose();
        _batcher.Dispose();
    }

    private void RegisterSystem<T>() where T : GameSystem, new()
    {
        var system = new T
        {
            App = this,
            Registry = _registry,
            Assets = _assets,
            Batcher = _batcher
        };

        _systems.Add(system);
        system.Startup();
    }
}