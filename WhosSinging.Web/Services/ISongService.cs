using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public interface ISongService
{
    Task<List<Song>> GetActiveSongsAsync();

    Task<Song?> GetByIdAsync(int id);

    Task<Song?> GetRandomSongAsync();

    Task<int> GetCountAsync();
}