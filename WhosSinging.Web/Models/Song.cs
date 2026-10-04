namespace WhosSinging.Web.Models;

public class Song
{
    public int Id { get; set; }

    public string titulo { get; set; } = string.Empty;

    public string artista { get; set; } = string.Empty;

    public int año { get; set; }

    public string archivo { get; set; } = string.Empty;

    public string youtube_id { get; set; } = string.Empty;

    public string genero { get; set; } = "desconocido";

    public int duracion_segundos { get; set; }

    // Campo interno de la app (no viene del JSON)
    public bool activo { get; set; } = true;
}