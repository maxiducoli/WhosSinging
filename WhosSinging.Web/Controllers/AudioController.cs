using Microsoft.AspNetCore.Mvc;
using WhosSinging.Web.Services;

namespace WhosSinging.Web.Controllers;

public class AudioController : Controller
{
    private readonly ISongService _songService;
    private readonly IAudioService _audioService;

    public AudioController(
        ISongService songService,
        IAudioService audioService)
    {
        _songService = songService;
        _audioService = audioService;
    }

    // GET: /audio/5
    [HttpGet("audio/{id:int}")]
    public async Task<IActionResult> GetAudio(int id)
    {
        // 1. Buscar la canción en la base de datos
        var song = await _songService.GetByIdAsync(id);
        if (song == null)
        {
            return NotFound("Canción no encontrada");
        }

        // 2. Resolver la ruta real del MP3 (con protección)
        var fullPath = _audioService.ResolveAudioPath(song.archivo);
        if (fullPath == null)
        {
            return NotFound("Archivo de audio no disponible");
        }

        // 3. Devolver el archivo (con caché de 7 días en el navegador)
        Response.Headers["Cache-Control"] = "public, max-age=604800";

        return PhysicalFile(fullPath, "audio/mpeg");
    }
}