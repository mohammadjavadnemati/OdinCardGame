using System.Collections.Concurrent;
using OdinGame.Models;

namespace OdinGame.Services
{
    public class RoomService
    {
        private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

        public GameRoom CreateRoom(string hostConnectionId, string hostName)
        {
            string code = GenerateRoomCode();

            var room = new GameRoom
            {
                RoomCode = code,
                HostConnectionId = hostConnectionId
            };

            room.Players.Add(new Player
            {
                ConnectionId = hostConnectionId,
                Name = hostName
            });

            _rooms[code] = room;
            return room;
        }

        public GameRoom? GetRoom(string roomCode)
        {
            _rooms.TryGetValue(roomCode, out var room);
            return room;
        }

        public bool JoinRoom(string roomCode, string connectionId, string playerName, out string? error)
        {
            error = null;
            var room = GetRoom(roomCode);

            if (room == null)
            {
                error = "روم پیدا نشد.";
                return false;
            }

            if (room.IsGameStarted)
            {
                error = "بازی این روم قبلاً شروع شده است.";
                return false;
            }

            if (room.Players.Count >= GameRoom.MaxPlayers)
            {
                error = "روم پر است.";
                return false;
            }

            room.Players.Add(new Player
            {
                ConnectionId = connectionId,
                Name = playerName
            });

            return true;
        }

        public void RemovePlayer(string connectionId)
        {
            foreach (var room in _rooms.Values)
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (player != null)
                {
                    room.Players.Remove(player);

                    if (room.Players.Count == 0)
                    {
                        _rooms.TryRemove(room.RoomCode, out _);
                    }
                }
            }
        }

        public GameRoom? GetRoomByConnectionId(string connectionId)
        {
            return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // بدون حروف/اعداد شبیه به هم
            var random = new Random();
            string code;

            do
            {
                code = new string(Enumerable.Range(0, 5).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            } while (_rooms.ContainsKey(code));

            return code;
        }
        public bool SetPlayerReady(string roomCode, string connectionId, bool isReady, out string? error)
        {
            error = null;
            var room = GetRoom(roomCode);

            if (room == null)
            {
                error = "روم پیدا نشد.";
                return false;
            }

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
            {
                error = "بازیکن پیدا نشد.";
                return false;
            }

            player.IsReady = isReady;
            return true;
        }
    }
}