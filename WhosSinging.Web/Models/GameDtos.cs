namespace WhosSinging.Web.Models;

// Lo que se le envía a los navegadores al empezar una ronda
public class RoundPayload
{
    public int Round { get; set; }
    public int TotalRounds { get; set; }
    public int SongId { get; set; }
    public string AudioUrl { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int TimeLimitSeconds { get; set; }
}

// El resultado de la respuesta de un jugador
public class AnswerOutcome
{
    public bool Correct { get; set; }
    public int PointsEarned { get; set; }
}

// El detalle de un jugador en el cierre de ronda
public class PlayerRoundResult
{
    public string Name { get; set; } = "";
    public bool Correct { get; set; }
    public int PointsEarned { get; set; }
    public int TotalScore { get; set; }
    public int Streak { get; set; }
}

// El resumen completo de una ronda
public class RoundResult
{
    public string CorrectTitle { get; set; } = "";
    public string CorrectArtist { get; set; } = "";
    public List<PlayerRoundResult> Results { get; set; } = new();
}