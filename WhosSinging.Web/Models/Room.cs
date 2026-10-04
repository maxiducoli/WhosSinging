namespace WhosSinging.Web.Models;

public class Room
{
    public string Code { get; set; } = string.Empty;

    public List<Player> Players { get; set; } = new();

    public bool GameStarted { get; set; }

    public int CurrentRound { get; set; }

    public int TotalRounds { get; set; } = 10;

    public Queue<int> SongQueue { get; set; } = new();

    public RoundState? CurrentRoundState { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}