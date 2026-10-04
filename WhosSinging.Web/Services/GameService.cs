using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public class GameService : IGameService
{
    private static readonly Random Rnd = new();

    public bool TryStartGame(Room room, List<Song> songs, out string? error)
    {
        error = null;

        lock (room)
        {
            if (room.GameStarted)
            {
                error = "El juego ya comenzo en esta sala.";
                return false;
            }

            if (songs.Count < 4)
            {
                error = "Se necesitan al menos 4 canciones para jugar.";
                return false;
            }

            if (!room.Players.Any(p => !p.IsReferee && p.IsConnected))
            {
                error = "Espera al menos un jugador conectado antes de comenzar.";
                return false;
            }

            room.GameStarted = true;
            room.CurrentRound = 0;

            // Barajar todas las canciones y tomar las primeras N
            var ids = songs
                .Select(s => s.Id)
                .OrderBy(_ => Rnd.Next())
                .ToList();

            room.SongQueue = new Queue<int>(
                ids.Take(Math.Min(room.TotalRounds, ids.Count)));

            return true;
        }
    }

    public bool TryStartRound(Room room, List<Song> songs, out RoundPayload? payload)
    {
        payload = null;

        lock (room)
        {
            if (room.SongQueue.Count == 0) return false;

            var songId = room.SongQueue.Dequeue();
            var song = songs.FirstOrDefault(s => s.Id == songId);
            if (song == null) return false;

            room.CurrentRound++;

            // 3 opciones falsas + la correcta, todo barajado
            var options = songs
                .Where(s => s.Id != songId)
                .Select(s => s.titulo)
                .OrderBy(_ => Rnd.Next())
                .Take(3)
                .ToList();

            options.Add(song.titulo);
            options = options.OrderBy(_ => Rnd.Next()).ToList();

            foreach (var p in room.Players)
            {
                p.HasAnswered = false;
            }

            room.CurrentRoundState = new RoundState
            {
                SongId = song.Id,
                CorrectTitle = song.titulo,
                CorrectArtist = song.artista,
                Options = options,
                TimeLimitSeconds = 20,
                StartedAtUtc = DateTime.UtcNow
            };

            payload = new RoundPayload
            {
                Round = room.CurrentRound,
                TotalRounds = room.TotalRounds,
                SongId = song.Id,
                AudioUrl = $"/audio/{song.Id}",
                Options = options,
                TimeLimitSeconds = 20
            };

            return true;
        }
    }

    public AnswerOutcome? SubmitAnswer(Room room, Player player, string answer)
    {

        if (player.IsReferee) return null;

        lock (room)
        {
            var round = room.CurrentRoundState;
            if (round == null || player.HasAnswered) return null;

            player.HasAnswered = true;

            var elapsed = (DateTime.UtcNow - round.StartedAtUtc).TotalSeconds;
            var correct = string.Equals(
                answer.Trim(),
                round.CorrectTitle,
                StringComparison.OrdinalIgnoreCase);

            var points = 0;

            if (correct)
            {
                // Acierto base
                points = 100;

                // Bonus por velocidad (hasta 50)
                if (elapsed <= round.TimeLimitSeconds)
                {
                    var fraction = 1 - (elapsed / round.TimeLimitSeconds);
                    points += (int)Math.Round(50 * fraction);
                }

                // Bonus por racha (hasta 50)
                player.Streak++;
                points += Math.Min((player.Streak - 1) * 10, 50);
            }
            else
            {
                player.Streak = 0;
            }

            player.Score += points;

            var outcome = new AnswerOutcome
            {
                Correct = correct,
                PointsEarned = points
            };

            round.Answers[player.ConnectionId] = outcome;
            return outcome;
        }
    }

    public bool AllAnswered(Room room)
    {
        lock (room)
        {
            var jugadores = room.Players.Where(p => !p.IsReferee).ToList();
            return jugadores.Count > 0 &&
                   jugadores.All(p => p.HasAnswered || !p.IsConnected);
        }
    }

    public RoundResult FinishRound(Room room)
    {
        lock (room)
        {
            var round = room.CurrentRoundState;
            var result = new RoundResult();
            if (round == null) return result;

            result.CorrectTitle = round.CorrectTitle;
            result.CorrectArtist = round.CorrectArtist;

            foreach (var player in room.Players.Where(p => !p.IsReferee))
            {
                round.Answers.TryGetValue(player.ConnectionId, out var outcome);

                result.Results.Add(new PlayerRoundResult
                {
                    Name = player.Name,
                    Correct = outcome?.Correct ?? false,
                    PointsEarned = outcome?.PointsEarned ?? 0,
                    TotalScore = player.Score,
                    Streak = player.Streak
                });
            }

            // Ranking parcial, ordenado por puntaje
            result.Results = result.Results
                .OrderByDescending(r => r.TotalScore)
                .ToList();

            room.CurrentRoundState = null;
            return result;
        }
    }

    public bool HasMoreRounds(Room room)
    {
        lock (room)
        {
            return room.SongQueue.Count > 0;
        }
    }
}