namespace WhosSinging.Web.Models;

public class Player
{
    public string ConnectionId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Score { get; set; }

    public int Streak { get; set; }

    public bool IsHost { get; set; }

    public bool HasAnswered { get; set; }

    public bool IsConnected { get; set; } = true;

    public DateTime? DisconnectedAtUtc { get; set; }

    public bool IsReferee { get; set; }

}