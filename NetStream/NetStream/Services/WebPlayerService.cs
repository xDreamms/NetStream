using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Serilog;

namespace NetStream.Services
{
    public class WebPlayerService : IPlayerService
    {
        private readonly IJSRuntime _jsRuntime;
        private bool _isInitialized = false;
        private string _currentVideoUrl;
        private string _elementId;

        public event Action<TimeSpan> PositionChanged;
        public event Action<TimeSpan> DurationChanged;
        public event Action PlaybackStarted;
        public event Action PlaybackPaused;
        public event Action PlaybackStopped;
        public event Action<string> Error;

        public WebPlayerService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task Initialize(string elementId)
        {
            try
            {
                _elementId = elementId;
                await _jsRuntime.InvokeVoidAsync("initializeVideoPlayer", elementId, DotNetObjectReference.Create(this));
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize web player: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Play(string url)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Player must be initialized before playing a video");
            }

            try
            {
                _currentVideoUrl = url;
                await _jsRuntime.InvokeVoidAsync("playVideo", _elementId, url);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to play video: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Pause()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("pauseVideo", _elementId);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to pause video: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Resume()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("resumeVideo", _elementId);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to resume video: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Stop()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("stopVideo", _elementId);
                _currentVideoUrl = null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop video: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Seek(TimeSpan position)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("seekVideo", _elementId, position.TotalSeconds);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to seek video: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task SetVolume(int volume)
        {
            try
            {
                // Volume should be between 0 and 100
                volume = Math.Clamp(volume, 0, 100);
                await _jsRuntime.InvokeVoidAsync("setVideoVolume", _elementId, volume / 100.0);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set volume: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task<TimeSpan> GetPosition()
        {
            try
            {
                double seconds = await _jsRuntime.InvokeAsync<double>("getVideoPosition", _elementId);
                return TimeSpan.FromSeconds(seconds);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get position: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task<TimeSpan> GetDuration()
        {
            try
            {
                double seconds = await _jsRuntime.InvokeAsync<double>("getVideoDuration", _elementId);
                return TimeSpan.FromSeconds(seconds);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get duration: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task SetFullScreen(bool fullScreen)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("setVideoFullScreen", _elementId, fullScreen);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set fullscreen: {ex.Message}");
                Error?.Invoke(ex.Message);
                throw;
            }
        }

        [JSInvokable]
        public void OnPositionChanged(double seconds)
        {
            PositionChanged?.Invoke(TimeSpan.FromSeconds(seconds));
        }

        [JSInvokable]
        public void OnDurationChanged(double seconds)
        {
            DurationChanged?.Invoke(TimeSpan.FromSeconds(seconds));
        }

        [JSInvokable]
        public void OnPlaybackStarted()
        {
            PlaybackStarted?.Invoke();
        }

        [JSInvokable]
        public void OnPlaybackPaused()
        {
            PlaybackPaused?.Invoke();
        }

        [JSInvokable]
        public void OnPlaybackStopped()
        {
            PlaybackStopped?.Invoke();
        }

        [JSInvokable]
        public void OnError(string message)
        {
            Error?.Invoke(message);
        }
    }
} 