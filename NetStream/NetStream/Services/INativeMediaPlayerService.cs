using Avalonia.Controls;

namespace NetStream.Services;

public interface INativeMediaPlayerService
{
    Control CreateControl();
    void Play(string uri);
}