using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Track.Ballast;

public class GameHostBuilder
{
    public ServiceCollection Services { get; } = new();
    public GameHostBuilder ConfigureGameDefaults() {
        Services.AddOptions()
            .AddSingleton<Time>()
            .AddSingleton<GameHostImpl>()
            .AddSingleton<IGameLifetime>(s => s.GetRequiredService<GameHostImpl>())
            .AddScoped<IGraphicsDeviceManager>(s => s.GetRequiredService<GameHostImpl>().GraphicsDeviceManager)
            .AddScoped<IGraphicsDeviceService>(s => s.GetRequiredService<GameHostImpl>().GraphicsDeviceManager)
            .AddScoped<ContentManager>(s => s.GetRequiredService<GameHostImpl>().Content);

        return this;
    }

    public GameHostBuilder WithGame<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IGame {
        Services.AddScoped<IGame, T>();
        return this;
    }

    public GameHostBuilder ConfigureServices(Action<GameHostBuilder, ServiceCollection> configure) {
        configure(this, Services);
        return this;
    }

    public GameHostBuilder WithGame(Func<IServiceProvider, IGame> implementationFactory) {
        Services.AddScoped(implementationFactory);
        return this;
    }

    public GameHost Build() => new(Services.BuildServiceProvider());
}

public sealed class GameHost : IDisposable
{
    internal GameHost(ServiceProvider services) {
        _impl = services.GetRequiredService<GameHostImpl>();
        Services = services;
    }
    public ServiceProvider Services { get; }
    private readonly GameHostImpl _impl;

    public void Dispose() {
        _impl.Dispose();
        Services.Dispose();
    }

    public void Run() {
        _impl.Run();
    }
}