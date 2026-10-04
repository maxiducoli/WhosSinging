using Microsoft.EntityFrameworkCore;
using WhosSinging.Web.Data;
using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public class SongService : ISongService
{
    private readonly WhosSingingDbContext _db;

    public SongService(WhosSingingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Song>> GetActiveSongsAsync()
    {
        return await _db.Songs
            .Where(s => s.activo)
            .ToListAsync();
    }

    public async Task<Song?> GetByIdAsync(int id)
    {
        return await _db.Songs.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Song?> GetRandomSongAsync()
    {
        var count = await _db.Songs.CountAsync(s => s.activo);
        if (count == 0) return null;

        var random = Random.Shared.Next(count);

        return await _db.Songs
            .Where(s => s.activo)
            .OrderBy(s => s.Id)
            .Skip(random)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _db.Songs.CountAsync(s => s.activo);
    }
}