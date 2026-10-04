namespace WhosSinging.Web.Models;

public class RoundState
{
    public int SongId { get; set; }

    public string CorrectTitle { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    public int TimeLimitSeconds { get; set; } = 20;

    public DateTime StartedAtUtc { get; set; }

    public string CorrectArtist { get; set; } = "";

    public Dictionary<string, AnswerOutcome> Answers { get; set; } = new();
}