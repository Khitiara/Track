namespace Track.Ballast;

public interface IGame
{
    void Initialize();
    void LoadContent();
    void UnloadContent();
    void Update();
    void Draw();
}