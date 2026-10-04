using System.Collections.Concurrent;
using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public class RoomManager : IRoomManager
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly Random Rnd = new();
    private static readonly TimeSpan Grace = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    public Room CreateRoom(string connectionId, string hostName, bool modoPresentador)
    {
        PurgeStaleRooms();

        string code;
        do
        {
            code = GenerateCode();
        }
        while (_rooms.ContainsKey(code));

        var room = new Room
        {
            Code = code,
            CreatedAt = DateTime.UtcNow
        };

        room.Players.Add(new Player
        {
            ConnectionId = connectionId,
            Name = hostName,
            IsHost = true,
            IsReferee = modoPresentador,
            IsConnected = true
        });

        _rooms[code] = room;
        return room;
    }

    public bool TryJoin(string code, string connectionId, string name, out Room? room)
    {
        room = null;
        code = code.Trim().ToUpperInvariant();

        PurgeStaleRooms();

        if (!_rooms.TryGetValue(code, out var existing))
            return false;

        lock (existing)
        {
            // Reconexión: alguien recupera su lugar desconectado (por nombre),
            // incluso si la partida ya empezó
            var returning = existing.Players.FirstOrDefault(
                p => !p.IsConnected &&
                     p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (returning != null)
            {
                returning.ConnectionId = connectionId;
                returning.IsConnected = true;
                returning.DisconnectedAtUtc = null;
                room = existing;
                return true;
            }

            // Jugador nuevo: no puede entrar a una partida que ya empezó
            if (existing.GameStarted)
                return false;

            existing.Players.Add(new Player
            {
                ConnectionId = connectionId,
                Name = name,
                IsConnected = true
            });

            room = existing;
            return true;
        }
    }

    public Room? GetRoom(string code)
    {
        return _rooms.TryGetValue(code.Trim().ToUpperInvariant(), out var room)
            ? room
            : null;
    }

    public Room? GetRoomByConnection(string connectionId)
    {
        foreach (var room in _rooms.Values)
        {
            lock (room)
            {
                if (room.Players.Any(p => p.ConnectionId == connectionId))
                    return room;
            }
        }
        return null;
    }

    public IEnumerable<string> GetRoomCodes()
    {
        return _rooms.Keys.ToList();
    }

    // Salida explícita (botón / cierre de pestaña): se va de verdad
    public void Leave(string connectionId)
    {
        var room = GetRoomByConnection(connectionId);
        if (room == null) return;

        lock (room)
        {
            room.Players.RemoveAll(p => p.ConnectionId == connectionId);

            if (room.Players.Count == 0)
            {
                _rooms.TryRemove(room.Code, out _);
                return;
            }

            if (!room.Players.Any(p => p.IsHost))
            {
                room.Players[0].IsHost = true;
            }
        }
    }

    // Caída de conexión: se marca, NO se borra (puede volver)
    public Room? MarkDisconnected(string connectionId)
    {
        var room = GetRoomByConnection(connectionId);
        if (room == null) return null;

        lock (room)
        {
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null) return null;

            player.IsConnected = false;
            player.DisconnectedAtUtc = DateTime.UtcNow;
        }

        return room;
    }

    // Limpieza: salas donde nadie está conectado desde hace más de la gracia
    private void PurgeStaleRooms()
    {
        var cutoff = DateTime.UtcNow.Subtract(Grace);

        foreach (var kvp in _rooms)
        {
            bool purge;
            lock (kvp.Value)
            {
                var players = kvp.Value.Players;
                purge = players.Count == 0 ||
                        (players.All(p => !p.IsConnected) &&
                         players.Max(p => p.DisconnectedAtUtc ?? DateTime.UtcNow) < cutoff);
            }

            if (purge)
            {
                _rooms.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string GenerateCode()
    {
        var chars = new char[5];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Rnd.Next(Alphabet.Length)];
        }
        return new string(chars);
    }
}