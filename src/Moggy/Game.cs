using Foster.Framework;
using System.Numerics;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Tools;
using Serilog;

namespace Moggy;

public enum GameState
{
    Level,
    Score
}

public class Game : App
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<Game>();

    public bool IsPaused { get; private set; } = true; // TODO: Debug only

    internal GameState State => _gameStates.State;

    private readonly Registry _registry = new();

    private GameSystemGroup _gameSystems = null!;

    private GameSystemGroup<GameState> _gameStates = null!;

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

        var levelSystems = new GameSystemGroup(
            CreateSystem<LevelRuntimeGameSystem>(),
            CreateSystem<LevelGameSystem>(),
            CreateSystem<ChaosSystem>(),
            CreateSystem<PlayerGameSystem>(),
            CreateSystem<AbilitiesGameSystem>(),
            CreateSystem<NormalSystem>(),
            CreateSystem<BigBoySystem>(),
            CreateSystem<MicroManSystem>(),
            CreateSystem<NavigationGameSystem>(),
            CreateSystem<EnemyGameSystem>(),
            CreateSystem<BugSystem>(),
            CreateSystem<LevelMoverSystem>(),
            CreateSystem<CameraFollowGameSystem>(),
            CreateSystem<CameraSystem>(),
            CreateSystem<SpriteSystem>(),
            CreateSystem<HudSystem>()
        );

        var scoreSystems = new GameSystemGroup(
            CreateSystem<ScoreScreenGameSystem>()
        );

        _gameStates = new GameSystemGroup<GameState>(
            GameState.Level,
            new Dictionary<GameState, GameSystemGroup>
            {
                [GameState.Level] = levelSystems,
                [GameState.Score] = scoreSystems
            });

        _gameSystems = new GameSystemGroup(
            CreateSystem<ViewportSystem>(),
            _gameStates);

        _gameSystems.Startup();

        _tools = new ToolsHost(this, _registry, _assets);
    }

    internal void TransitionTo(GameState state)
    {
        _gameStates.TransitionTo(state);
    }

    protected override void Update()
    {
        if (Input.Keyboard.Pressed(Keys.Escape))
        {
            IsPaused = !IsPaused;
        }

        if (State != GameState.Level || !IsPaused)
        {
            _gameSystems.Update(Time);
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
        _gameSystems.Render(Time);

        _batcher.PopMatrix();
        _batcher.PopScissor();
        _tools.Draw(Time);
        _imgui.EndLayout();

        _batcher.Render(_screen);
        _batcher.Clear();

        Window.Clear(new Color(0.2f, 0.2f, 0.294f, 1f));

        _batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
        _batcher.Image(
            texture: _screen,
            position: viewport.WindowBounds.CenterF,
            origin: viewport.Origin,
            scale: Vector2.One * viewport.Scale,
            rotation: 0,
            color: Color.White
        );
        _batcher.PopSampler();

        _batcher.Render(Window);
        _batcher.Clear();

        _imgui.Render();
    }

    protected override void Shutdown()
    {
        _gameSystems.Shutdown();
        _assets.Dispose();
        _imgui.Dispose();
        _screen.Dispose();
        _batcher.Dispose();
    }

    private T CreateSystem<T>() where T : GameSystem, new()
    {
        return new T
        {
            Game = this,
            Registry = _registry,
            Assets = _assets,
            Batcher = _batcher
        };
    }
}
