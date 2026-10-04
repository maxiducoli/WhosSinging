using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public interface IGameService
{
    bool TryStartGame(Room room, List<Song> songs, out string? error);

    bool TryStartRound(Room room, List<Song> songs, out RoundPayload? payload);

    AnswerOutcome? SubmitAnswer(Room room, Player player, string answer);

    bool AllAnswered(Room room);

    RoundResult FinishRound(Room room);

    bool HasMoreRounds(Room room);
}