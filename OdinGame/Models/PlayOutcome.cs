namespace OdinGame.Models
{
    public class PlayOutcome
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public bool NeedsCardChoice { get; set; }
        public bool RoundEnded { get; set; }
        public bool PlayerEmptiedHand { get; set; }
    }
}