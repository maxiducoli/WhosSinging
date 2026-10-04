using Microsoft.Extensions.Options;
using WhosSinging.Web.Configuration;

namespace WhosSinging.Web.Services;

public class AudioService : IAudioService
{
    private readonly string _basePath;
    private readonly ILogger<AudioService> _logger;

    public AudioService(
        IOptions<MusicLibraryOptions> options,
        ILogger<AudioService> logger)
    {
        _basePath = Path.GetFullPath(options.Value.BasePath);
        _logger = logger;
    }

    public string? ResolveAudioPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        // Combinar y resolver la ruta completa
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        // SEGURIDAD: la ruta debe seguir dentro de _basePath
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Intento de path traversal bloqueado: {Path}", relativePath);
            return null;
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Archivo de audio no encontrado: {Path}", fullPath);
            return null;
        }

        return fullPath;
    }
}