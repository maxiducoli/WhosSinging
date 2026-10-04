using WhosSinging.Web.Models;

namespace WhosSinging.Web.Services;

public interface IRoomManager
{
    Room CreateRoom(string connectionId, string hostName, bool modoPresentador);

    bool TryJoin(string code, string connectionId, string name, out Room? room);

    Room? GetRoom(string code);

    Room? GetRoomByConnection(string connectionId);

    void Leave(string connectionId);

    Room? MarkDisconnected(string connectionId);

    IEnumerable<string> GetRoomCodes();
}