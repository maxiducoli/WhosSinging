using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WhosSinging.Web.Configuration;
using WhosSinging.Web.Data;
using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public class ImportService : IImportService
{
    private readonly WhosSingingDbContext _db;
    private readonly MusicLibraryOptions _options;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        WhosSingingDbContext db,
        IOptions<MusicLibraryOptions> options,
        ILogger<ImportService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ImportAllAsync()
    {
        if (!Directory.Exists(_options.BasePath))
        {
            _logger.LogWarning("La carpeta de música no existe: {Path}", _options.BasePath);
            return 0;
        }

        var jsonFiles = Directory.GetFiles(
            _options.BasePath,
            _options.DatabaseFileName,
            SearchOption.AllDirectories);

        int totalImported = 0;

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                int count = await ImportSingleFileAsync(jsonFile);
                totalImported += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando {File}", jsonFile);
            }
        }

        return totalImported;
    }

    private async Task<int> ImportSingleFileAsync(string jsonFilePath)
    {
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);

        // NUEVO: limpiar espacios sobrantes antes de leer
        jsonContent = NormalizeJson(jsonContent);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var database = JsonSerializer.Deserialize<GameDatabase>(jsonContent, options);

        if (database?.canciones == null || database.canciones.Count == 0)
        {
            _logger.LogInformation("Sin canciones en {File}", jsonFilePath);
            return 0;
        }

        int imported = 0;
        int skipped = 0;

        foreach (var song in database.canciones)
        {
            if (string.IsNullOrWhiteSpace(song.titulo) ||
                string.IsNullOrWhiteSpace(song.archivo))
            {
                skipped++;
                continue;
            }

            var existente = await _db.Songs.FirstOrDefaultAsync(s => s.youtube_id == song.youtube_id);

            if (existente != null)
            {
                // Ya existe: actualizamos sus datos (renombres, años corregidos, etc.)
                existente.titulo = song.titulo;
                existente.artista = song.artista;
                existente.año = song.año;
                existente.archivo = song.archivo;
                existente.genero = song.genero;
                existente.duracion_segundos = song.duracion_segundos;
                skipped++;
                continue;
            }

            song.Id = 0;
            song.activo = true;

            _db.Songs.Add(song);
            imported++;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "{File}: {Imported} importadas, {Skipped} duplicadas",
            Path.GetFileName(jsonFilePath), imported, skipped);

        return imported;
    }

    // ===== NUEVO: normalizador de JSON =====

    private static string NormalizeJson(string raw)
    {
        try
        {
            var node = JsonNode.Parse(raw);
            var cleaned = CleanNode(node);
            return cleaned.ToJsonString();
        }
        catch
        {
            // Si no se puede normalizar, se devuelve tal cual
            return raw;
        }
    }

    private static JsonNode? CleanNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var newObj = new JsonObject();
                foreach (var pair in obj)
                {
                    newObj[pair.Key.Trim()] = CleanNode(pair.Value);
                }
                return newObj;

            case JsonArray arr:
                var newArr = new JsonArray();
                foreach (var item in arr)
                {
                    newArr.Add(CleanNode(item));
                }
                return newArr;

            case JsonValue val when val.TryGetValue<string>(out var text):
                return JsonValue.Create(text.Trim());

            default:
                // Números, booleanos y null:
                // se recrean como nodos nuevos usando su propio texto JSON
                return node is null ? null : JsonNode.Parse(node.ToJsonString());
        }
    }
}