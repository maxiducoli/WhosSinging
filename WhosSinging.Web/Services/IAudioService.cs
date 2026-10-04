namespace WhosSinging.Web.Services;

public interface IAudioService
{
    string? ResolveAudioPath(string relativePath);
}