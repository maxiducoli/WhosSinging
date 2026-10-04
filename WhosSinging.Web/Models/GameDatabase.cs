namespace WhosSinging.Web.Models;

public class GameDatabase
{
    public string version { get; set; } = "1.0";

    public int total_canciones { get; set; }

    public List<Song> canciones { get; set; } = new List<Song>();
}