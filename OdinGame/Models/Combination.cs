namespace OdinGame.Models
{
    public class Combination
    {
        public List<Card> Cards { get; set; } = new();
        public int Value { get; private set; }

        public static Combination? TryCreate(List<Card> cards)
        {
            if (cards == null || cards.Count == 0)
                return null;

            if (cards.Count == 1)
            {
                return new Combination
                {
                    Cards = cards,
                    Value = cards[0].Number
                };
            }

            bool sameNumber = cards.All(c => c.Number == cards[0].Number);
            bool sameColor = cards.All(c => c.Color == cards[0].Color);

            if (!sameNumber && !sameColor)
                return null; // ترکیب نامعتبر

            // در هر دو حالت (same-number یا same-color)، نزولی مرتب و به هم می‌چسبونیم
            var ordered = cards.OrderByDescending(c => c.Number).ToList();
            var value = int.Parse(string.Concat(ordered.Select(c => c.Number)));

            return new Combination
            {
                Cards = ordered,
                Value = value
            };
        }
    }
}