namespace NetStream.Services;

public interface IVideoPlayerService
{
    void Initialize();
    void Play(string url);
    void Pause();
    void Stop();
    bool IsPlaying { get; }
}