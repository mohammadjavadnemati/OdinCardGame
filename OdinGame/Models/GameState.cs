namespace OdinGame.Models
{
    public class GameState
    {
        public List<Player> Players { get; set; } = new();
        public int CurrentPlayerIndex { get; set; }
        public Combination? CurrentCombination { get; set; }
        public string? LastPlayerToPlayId { get; set; } // ConnectionId
        public int ConsecutivePasses { get; set; } = 0;
        public bool AwaitingCardChoice { get; set; } = false;
        public List<Card> CardsAwaitingChoice { get; set; } = new();
        public string? PlayerAwaitingChoiceId { get; set; }

        public void StartNewGame()
        {
            var deck = GenerateDeck();
            Shuffle(deck);

            foreach (var player in Players)
            {
                player.Hand.Clear();
                player.HasPassed = false;
            }

            int cardsPerPlayer = 9;
            int deckIndex = 0;

            foreach (var player in Players)
            {
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    player.Hand.Add(deck[deckIndex]);
                    deckIndex++;
                }
            }

            CurrentCombination = null;
            LastPlayerToPlayId = null;
            CurrentPlayerIndex = new Random().Next(Players.Count);
        }

        private List<Card> GenerateDeck()
        {
            var deck = new List<Card>();
            foreach (CardColor color in Enum.GetValues<CardColor>())
            {
                for (int number = 1; number <= 9; number++)
                {
                    deck.Add(new Card(color, number));
                }
            }
            return deck; // 54 کارت
        }

        private void Shuffle(List<Card> deck)
        {
            var rng = new Random();
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (deck[k], deck[n]) = (deck[n], deck[k]);
            }
        }

        public bool IsValidPlay(List<Card> cardsToPlay)
        {
            var newCombo = Combination.TryCreate(cardsToPlay);
            if (newCombo == null) return false;

            if (CurrentCombination == null)
            {
                // اولین بازی راند: باید دقیقاً 1 کارت باشه
                return cardsToPlay.Count == 1;
            }

            int prevCount = CurrentCombination.Cards.Count;
            int newCount = cardsToPlay.Count;

            bool validCount = newCount == prevCount || newCount == prevCount + 1;
            bool validValue = newCombo.Value > CurrentCombination.Value;

            return validCount && validValue;
        }

        public Player CurrentPlayer => Players[CurrentPlayerIndex];

        public void AdvanceTurn()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        public PlayOutcome ProcessPlay(string playerToken, List<Card> cardsToPlay)
        {
            var player = Players.FirstOrDefault(p => p.PlayerToken == playerToken);
            if (player == null || CurrentPlayer.PlayerToken != playerToken)
                return new PlayOutcome { Success = false, Error = "نوبت شما نیست." };

            foreach (var c in cardsToPlay)
            {
                if (!player.Hand.Any(h => h.Color == c.Color && h.Number == c.Number))
                    return new PlayOutcome { Success = false, Error = "این کارت را در دست ندارید." };
            }

            if (!IsValidPlay(cardsToPlay))
                return new PlayOutcome { Success = false, Error = "ترکیب نامعتبر یا مقدار کافی نیست." };

            var newCombo = Combination.TryCreate(cardsToPlay)!;

            foreach (var c in cardsToPlay)
            {
                var match = player.Hand.First(h => h.Color == c.Color && h.Number == c.Number);
                player.Hand.Remove(match);
            }

            var previousCombo = CurrentCombination;
            CurrentCombination = newCombo;
            LastPlayerToPlayId = player.PlayerToken;
            ConsecutivePasses = 0;
            foreach (var p in Players) p.HasPassed = false;

            if (previousCombo != null && previousCombo.Cards.Count > 1)
            {
                AwaitingCardChoice = true;
                CardsAwaitingChoice = previousCombo.Cards;
                PlayerAwaitingChoiceId = player.PlayerToken;
                return new PlayOutcome { Success = true, NeedsCardChoice = true };
            }

            if (previousCombo != null && previousCombo.Cards.Count == 1)
            {
                player.Hand.Add(previousCombo.Cards[0]);
            }

            return FinalizeTurn(player);
        }

        public PlayOutcome FinalizeTurn(Player player)
        {
            if (player.Hand.Count == 0)
                return new PlayOutcome { Success = true, PlayerEmptiedHand = true, RoundEnded = true };

            AdvanceTurn();
            return new PlayOutcome { Success = true };
        }

        public PlayOutcome ChooseCardToTake(string playerToken, Card chosenCard)
        {
            if (!AwaitingCardChoice || PlayerAwaitingChoiceId != playerToken)
                return new PlayOutcome { Success = false, Error = "در انتظار انتخاب کارت نیستید." };

            if (!CardsAwaitingChoice.Any(c => c.Color == chosenCard.Color && c.Number == chosenCard.Number))
                return new PlayOutcome { Success = false, Error = "این کارت در ترکیب قبلی نبود." };

            var player = Players.First(p => p.PlayerToken == playerToken);
            player.Hand.Add(chosenCard);

            AwaitingCardChoice = false;
            CardsAwaitingChoice = new();
            PlayerAwaitingChoiceId = null;

            return FinalizeTurn(player);
        }

        public PlayOutcome ProcessPass(string playerToken)
        {
            var player = Players.FirstOrDefault(p => p.PlayerToken == playerToken);
            if (player == null || CurrentPlayer.PlayerToken != playerToken)
                return new PlayOutcome { Success = false, Error = "نوبت شما نیست." };

            if (CurrentCombination == null)
                return new PlayOutcome { Success = false, Error = "اولین بازی راند نمی‌تواند پاس باشد." };

            player.HasPassed = true;
            ConsecutivePasses++;

            if (ConsecutivePasses >= Players.Count - 1)
                return new PlayOutcome { Success = true, RoundEnded = true };

            AdvanceTurn();
            return new PlayOutcome { Success = true };
        }
    }
}