namespace OdinGame.Models
{
    public class GameRoom
    {
        public string RoomCode { get; set; } = string.Empty;
        public List<Player> Players { get; set; } = new();
        public GameState? Game { get; set; }
        public bool IsGameStarted { get; set; } = false;
        public string HostConnectionId { get; set; } = string.Empty;

        public const int MinPlayers = 3;
        public const int MaxPlayers = 6;

        public bool CanStart => Players.Count >= MinPlayers && Players.Count <= MaxPlayers;
        public bool AllPlayersReady => Players.Count >= MinPlayers && Players.All(p => p.IsReady);
    }
}