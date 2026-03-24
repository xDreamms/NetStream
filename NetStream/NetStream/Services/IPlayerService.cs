using System;
using System.Threading.Tasks;

namespace NetStream.Services
{
    public interface IPlayerService
    {
        event Action<TimeSpan> PositionChanged;
        event Action<TimeSpan> DurationChanged;
        event Action PlaybackStarted;
        event Action PlaybackPaused;
        event Action PlaybackStopped;
        event Action<string> Error;

        Task Initialize(string elementId);
        Task Play(string url);
        Task Pause();
        Task Resume();
        Task Stop();
        Task Seek(TimeSpan position);
        Task SetVolume(int volume);
        Task<TimeSpan> GetPosition();
        Task<TimeSpan> GetDuration();
        Task SetFullScreen(bool fullScreen);
    }
} 