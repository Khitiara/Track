using Microsoft.Xna.Framework;

namespace Track.Ballast;

public interface IGameLifetime
{
    public void Exit();
}

public interface IGameWindowHolder
{
    public GameWindow Window { get; }
    public bool IsMouseVisible { get; set; }

    public bool IsActive { get; }
}