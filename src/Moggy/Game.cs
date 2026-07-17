using Foster.Framework;
using System.Numerics;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Tools;
using Serilog;

namespace Moggy;

public enum GameState
{
    Active,
    Paused
}

public class Game : App
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<Game>();

    public GameState State { get; private set; } = GameState.Paused;

    private readonly List<GameSystem> _systems = new();

    private readonly Registry _registry = new();

    private AssetLoader _assets = null!;

    private Entity _view = Entity.Invalid;

    private Target _screen = null!;

    private Batcher _batcher = null!;

    private ImGuiRenderer _imgui = null!;

    private ToolsHost _tools = null!;

    public static void Main()
    {
        Logging.Initialize();
        Game? game = null;
        try
        {
            game = new Game();
            game.Run();
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception, "Application terminated unexpectedly");
        }
        finally
        {
            if (game is { Running: false, Disposed: false })
            {
                game.Dispose();
            }

            Foster.Framework.Log.SetCallbacks(null, null, null);
            Serilog.Log.CloseAndFlush();
        }
    }

    private Game() : base(new AppConfig
    {
        ApplicationName = "Moggy",
        WindowTitle = $"Moggy ({ThisAssembly.Git.Commit})",
        Width = 1280,
        Height = 720,
        Resizable = true
    })
    {
    }

    protected override void Startup()
    {
        _view = _registry.Create(
            new Viewport(Mathz.VirtualWidth, Mathz.VirtualHeight, Mathz.ViewportSize),
            new Camera(Vector2.Zero));
        _screen = new Target(GraphicsDevice, Mathz.VirtualWidth, Mathz.VirtualHeight, "GameScreen");
        _batcher = new Batcher(GraphicsDevice);
        _imgui = new ImGuiRenderer(this);

        _assets = new AssetLoader(this);
        RegisterSystem<ViewportSystem>();
        RegisterSystem<LevelSystem>();
        RegisterSystem<PlayerSystem>();
        RegisterSystem<NavigationSystem>();
        RegisterSystem<EnemySystem>();
        RegisterSystem<LevelMoverSystem>();
        RegisterSystem<CameraFollowSystem>();
        RegisterSystem<CameraSystem>();
        RegisterSystem<SpriteSystem>();
        RegisterSystem<HudSystem>();

        _tools = new ToolsHost(this, _registry, _assets);
    }

    protected override void Update()
    {
        if (Input.Keyboard.Pressed(Keys.Escape))
        {
            State = State == GameState.Paused ? GameState.Active : GameState.Paused;
        }

        if (State == GameState.Active)
        {
            foreach (var system in _systems)
            {
                system.Update(Time);
            }
        }
    }

    protected override void Render()
    {
        ref var viewport = ref _registry.Get<Viewport>(_view);
        ref var camera = ref _registry.Get<Camera>(_view);

        _imgui.BeginLayout();

        _screen.Clear(Color.Black);
        _batcher.PushScissor(viewport.ContentBounds);
        _batcher.PushMatrix(camera.WorldToVirtual);
        foreach (var system in _systems)
        {
            system.Render(Time);
        }

        _batcher.PopMatrix();
        _batcher.PopScissor();
        _tools.Draw(Time);
        _imgui.EndLayout();

        _batcher.Render(_screen);
        _batcher.Clear();

        Window.Clear(new Color(0.2f, 0.2f, 0.294f, 1f));

        _batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
        _batcher.Image(_screen, viewport.WindowBounds.CenterF, viewport.Origin, Vector2.One * viewport.Scale, 0, Color.White);
        _batcher.PopSampler();

        _batcher.Render(Window);
        _batcher.Clear();

        _imgui.Render();
    }

    protected override void Shutdown()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }

        _systems.Clear();
        _assets.Dispose();
        _imgui.Dispose();
        _screen.Dispose();
        _batcher.Dispose();
    }

    private void RegisterSystem<T>() where T : GameSystem, new()
    {
        var system = new T
        {
            Game = this,
            Registry = _registry,
            Assets = _assets,
            Batcher = _batcher
        };

        _systems.Add(system);
        system.Startup();
    }
}
