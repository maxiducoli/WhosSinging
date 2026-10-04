using Microsoft.AspNetCore.SignalR;
using WhosSinging.Web.Models;
using WhosSinging.Web.Services;

namespace WhosSinging.Web.Hubs;

public class GameHub : Hub
{
    private readonly IRoomManager _rooms;
    private readonly IGameService _game;
    private readonly ISongService _songs;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IRoomManager rooms,
        IGameService game,
        ISongService songs,
        ILogger<GameHub> logger)
    {
        _rooms = rooms;
        _game = game;
        _songs = songs;
        _logger = logger;
    }

    // ================= SALAS =================

    public async Task CreateRoom(string playerName, bool modoPresentador)
    {
        playerName = SanitizeName(playerName);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            await Clients.Caller.SendAsync("Error", "Ponete un nombre para jugar.");
            return;
        }

        var room = _rooms.CreateRoom(Context.ConnectionId, playerName, modoPresentador);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        _logger.LogInformation("Sala {Code} creada por {Name} (presentador: {Ref})",
            room.Code, playerName, modoPresentador);

        await Clients.Caller.SendAsync("RoomCreated", new
        {
            roomCode = room.Code,
            isHost = true,
            isReferee = modoPresentador
        });

        await BroadcastRoomUpdated(room);
    }

    public async Task JoinRoom(string code, string playerName)
    {
        playerName = SanitizeName(playerName);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            await Clients.Caller.SendAsync("Error", "Ponete un nombre para jugar.");
            return;
        }

        var preexisting = _rooms.GetRoom(code);
        if (preexisting == null)
        {
            _logger.LogWarning(
                "Union fallida de {Name} a {Code}: la sala no existe. Salas activas: [{Salas}]",
                playerName, code, string.Join(",", _rooms.GetRoomCodes()));

            await Clients.Caller.SendAsync("Error",
                "La sala no existe en este servidor. Pidele al anfitrion el codigo de SU pantalla actual.");
            return;
        }

        if (!_rooms.TryJoin(code, Context.ConnectionId, playerName, out var room) || room == null)
        {
            _logger.LogWarning(
                "Union fallida de {Name} a {Code}: la partida ya comenzo",
                playerName, code);

            await Clients.Caller.SendAsync("Error",
                "La partida ya empezo en esa sala. Espera la proxima partida.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        _logger.LogInformation("{Name} se unio a la sala {Code}", playerName, room.Code);

        await Clients.Caller.SendAsync("JoinedRoom", new
        {
            roomCode = room.Code,
            isHost = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.IsHost ?? false,
            isReferee = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.IsReferee ?? false
        });

        await BroadcastRoomUpdated(room);
    }

    public async Task LeaveRoom()
    {
        await HandleExplicitLeave(Context.ConnectionId);
    }

    // Caida de conexion: marcamos, no borramos
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _rooms.MarkDisconnected(Context.ConnectionId);

        if (room != null)
        {
            _logger.LogInformation("Conexion caida en sala {Code}", room.Code);

            await BroadcastRoomUpdated(room);

            // Si estaba en ronda y los restantes ya respondieron, cerramos
            if (room.GameStarted &&
                room.CurrentRoundState != null &&
                _game.AllAnswered(room))
            {
                await BroadcastRoundResultAsync(room);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ================= JUEGO =================

    public async Task StartGame()
    {
        var room = _rooms.GetRoomByConnection(Context.ConnectionId);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "No estas en ninguna sala.");
            return;
        }

        if (!IsHost(room, Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("Error", "Solo el anfitrion puede comenzar el juego.");
            return;
        }

        var songs = await _songs.GetActiveSongsAsync();

        if (!_game.TryStartGame(room, songs, out var error))
        {
            await Clients.Caller.SendAsync("Error", error ?? "No se pudo comenzar el juego.");
            return;
        }

        _logger.LogInformation("Partida iniciada en la sala {Code}", room.Code);

        await StartAndBroadcastRoundAsync(room, songs);
    }

    public async Task SubmitAnswer(string answer)
    {
        var room = _rooms.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return;

        Player? player;
        lock (room)
        {
            player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        }
        if (player == null) return;

        if (player.IsReferee)
        {
            await Clients.Caller.SendAsync("Error", "El presentador no puede responder.");
            return;
        }

        var outcome = _game.SubmitAnswer(room, player, answer);
        if (outcome == null)
        {
            // Silencioso: la ronda ya se cerró o ya habías respondido
            return;
        }

        await Clients.Caller.SendAsync("AnswerReceived");

        int respondieron, total;
        lock (room)
        {
            total = room.Players.Count(p => !p.IsReferee);
            respondieron = room.Players.Count(p => !p.IsReferee && (p.HasAnswered || !p.IsConnected));
        }
        await Clients.Group(room.Code).SendAsync("AnswerProgress", respondieron, total);

        if (_game.AllAnswered(room))
        {
            await BroadcastRoundResultAsync(room);
        }
    }

    public async Task FinishRoundNow()
    {
        var room = _rooms.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return;

        if (!IsHost(room, Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("Error", "Solo el anfitrion puede cerrar la ronda.");
            return;
        }

        await BroadcastRoundResultAsync(room);
    }

    public async Task NextRound()
    {
        var room = _rooms.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return;

        if (!IsHost(room, Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("Error", "Solo el anfitrion puede pasar de ronda.");
            return;
        }

        if (_game.HasMoreRounds(room))
        {
            var songs = await _songs.GetActiveSongsAsync();
            await StartAndBroadcastRoundAsync(room, songs);
        }
        else
        {
            await BroadcastGameFinishedAsync(room);
        }
    }

    public async Task Rematch()
    {
        var room = _rooms.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return;

        if (!IsHost(room, Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("Error", "Solo el anfitrion puede pedir revancha.");
            return;
        }

        lock (room)
        {
            room.GameStarted = false;
            room.CurrentRound = 0;
            room.SongQueue.Clear();
            room.CurrentRoundState = null;

            foreach (var p in room.Players)
            {
                p.Score = 0;
                p.Streak = 0;
                p.HasAnswered = false;
            }
        }

        await Clients.Group(room.Code).SendAsync("RematchReady");
        await BroadcastRoomUpdated(room);
    }

    // ================= PRIVADOS =================

    private bool IsHost(Room room, string connectionId)
    {
        lock (room)
        {
            return room.Players.Any(p => p.ConnectionId == connectionId && p.IsHost);
        }
    }

    private async Task StartAndBroadcastRoundAsync(Room room, List<Song> songs)
    {
        if (!_game.TryStartRound(room, songs, out var payload) || payload == null)
        {
            await Clients.Group(room.Code).SendAsync("Error", "No se pudo iniciar la ronda.");
            return;
        }

        await Clients.Group(room.Code).SendAsync("RoundStarted", payload);


        int total;
        lock (room)
        {
            total = room.Players.Count(p => !p.IsReferee);
        }
        await Clients.Group(room.Code).SendAsync("AnswerProgress", 0, total);
    }

    private async Task BroadcastRoundResultAsync(Room room)
    {
        var result = _game.FinishRound(room);
        await Clients.Group(room.Code).SendAsync("RoundResult", result);
    }

    private async Task HandleExplicitLeave(string connectionId)
    {
        var room = _rooms.GetRoomByConnection(connectionId);
        if (room == null) return;

        var leavingName = room.Players
            .FirstOrDefault(p => p.ConnectionId == connectionId)?.Name;

        _rooms.Leave(connectionId);

        var updated = _rooms.GetRoom(room.Code);
        if (updated == null) return;

        if (!string.IsNullOrEmpty(leavingName))
        {
            await Clients.Group(updated.Code).SendAsync("PlayerLeft", leavingName);
        }

        await BroadcastRoomUpdated(updated);

        if (updated.GameStarted &&
            updated.CurrentRoundState != null &&
            _game.AllAnswered(updated))
        {
            await BroadcastRoundResultAsync(updated);
        }
    }

    private async Task BroadcastRoomUpdated(Room room)
    {
        await Clients.Group(room.Code).SendAsync("RoomUpdated", new
        {
            roomCode = room.Code,
            gameStarted = room.GameStarted,
            players = room.Players.Select(p => new
            {
                name = p.Name,
                score = p.Score,
                isHost = p.IsHost,
                isReferee = p.IsReferee,
                connected = p.IsConnected
            }).ToList()
        });
    }

    private async Task BroadcastGameFinishedAsync(Room room)
    {
        List<object> standings;
        lock (room)
        {
            standings = room.Players
                .Where(p => !p.IsReferee)
                .OrderByDescending(p => p.Score)
                .Select(p => (object)new { name = p.Name, score = p.Score })
                .ToList();
        }

        await Clients.Group(room.Code).SendAsync("GameFinished", standings);
    }
   
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        name = name.Trim();
        if (name.Length > 20) name = name.Substring(0, 20);
        return name;
    }
}