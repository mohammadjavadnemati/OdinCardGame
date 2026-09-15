namespace OdinGame.Models
{
    public enum CardColor
    {
        Pink,
        Blue,
        Red,
        Brown,
        Orange,
        Yellow
    }

    public class Card
    {
        public CardColor Color { get; set; }
        public int Number { get; set; } // 1 to 9

        public Card(CardColor color, int number)
        {
            Color = color;
            Number = number;
        }

        public override string ToString() => $"{Color} {Number}";

        public override bool Equals(object? obj)
        {
            if (obj is not Card other) return false;
            return Color == other.Color && Number == other.Number;
        }

        public override int GetHashCode() => HashCode.Combine(Color, Number);
    }
}