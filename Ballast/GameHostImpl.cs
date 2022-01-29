using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Track.Ties.Threading;

namespace Track.Ties;

internal class GameHostImpl : Game
{
    private readonly IServiceScopeFactory _scopeFactory;
    private          IGame?               _game;
    private readonly CoroutineManager     _coroutineManager;
    private          IServiceScope?       _scope;
    private readonly Time                 _time;
    public GraphicsDeviceManager GraphicsDeviceManager { get; }

    public GameHostImpl(IOptions<GameHostOptions> optionsContainer, IServiceScopeFactory scopeFactory, Time time) {
        _time = time;
        _coroutineManager = new CoroutineManager(_time);
        _scopeFactory = scopeFactory;
        GameHostOptions options = optionsContainer.Value;
        GraphicsDeviceManager = new GraphicsDeviceManager(this) {
            PreferredBackBufferWidth = options.Width,
            PreferredBackBufferHeight = options.Height,
            IsFullScreen = options.IsFullScreen,
            SynchronizeWithVerticalRetrace = true,
            PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
        };
        GraphicsDeviceManager.DeviceReset += OnGraphicsDeviceReset;
        Exiting += DisposeScope;
    }

    private void DisposeScope(object? sender, EventArgs e) {
        _game = null;
        _scope?.Dispose();
    }

    private void OnGraphicsDeviceReset(object? sender, EventArgs e) { }

    protected override void Initialize() {
        base.Initialize();

        _scope = _scopeFactory.CreateScope();
        _game = _scope.ServiceProvider.GetRequiredService<IGame>();
        _coroutineManager.Initialize();
    }

    protected override void Update(GameTime gameTime) {
        _time.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
        _coroutineManager.Update();
        _game!.Update();
    }

    protected override void Draw(GameTime gameTime) {
        base.Draw(gameTime);
        _game!.Draw();
    }
}