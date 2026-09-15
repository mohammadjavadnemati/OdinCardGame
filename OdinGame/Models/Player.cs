namespace OdinGame.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Card> Hand { get; set; } = new();
        public bool HasPassed { get; set; } = false;
        public bool IsReady { get; set; } = false;
        public int TotalScore { get; set; } = 0;
        public string PlayerToken { get; set; } = Guid.NewGuid().ToString();
        public bool IsConnected { get; set; } = true;
    }
}