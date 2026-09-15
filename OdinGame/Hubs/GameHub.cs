using Microsoft.AspNetCore.SignalR;
using OdinGame.Services;
using OdinGame.Models;

namespace OdinGame.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomService _roomService;
        private string? GetMyToken(Models.GameRoom room)
        {
            return room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.PlayerToken;
        }

        public GameHub(RoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task CreateRoom(string playerName)
        {
            var room = _roomService.CreateRoom(Context.ConnectionId, playerName);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
            await Clients.Caller.SendAsync("RoomCreated", room.RoomCode);
            await Clients.Group(room.RoomCode).SendAsync("PlayerListUpdated", GetPlayerNames(room));
        }

        public async Task JoinRoom(string roomCode, string playerName)
        {
            bool success = _roomService.JoinRoom(roomCode, Context.ConnectionId, playerName, out string? error);

            if (!success)
            {
                await Clients.Caller.SendAsync("JoinFailed", error);
                return;
            }

            var room = _roomService.GetRoom(roomCode)!;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("RoomJoined", roomCode);
            await Clients.Group(roomCode).SendAsync("PlayerListUpdated", GetPlayerNames(room));
        }
        public async Task SetReady(bool isReady)
        {
            var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
            if (room == null) return;

            _roomService.SetPlayerReady(room.RoomCode, Context.ConnectionId, isReady, out _);

            await Clients.Group(room.RoomCode).SendAsync("PlayerReadyUpdated",
                room.Players.Select(p => new { p.Name, p.IsReady }));

            if (room.AllPlayersReady && !room.IsGameStarted)
            {
                StartGame(room);
            }
        }

        private void StartGame(Models.GameRoom room)
        {
            room.IsGameStarted = true;
            room.Game = new Models.GameState { Players = room.Players };
            room.Game.StartNewGame();

            var startingPlayer = room.Game.CurrentPlayer;

            foreach (var player in room.Players)
            {
                Clients.Client(player.ConnectionId).SendAsync("GameStarted", new
                {
                    YourHand = player.Hand.Select(c => new { Color = c.Color.ToString(), c.Number }),
                    CurrentPlayerName = startingPlayer.Name,
                    AllPlayers = room.Players.Select(p => new { p.Name, CardCount = p.Hand.Count })
                });
            }
        }
        public async Task PlayCards(List<CardDto> cards)
        {
            var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
            if (room?.Game == null) return;

            var cardList = cards.Select(c => new Models.Card(Enum.Parse<Models.CardColor>(c.Color), c.Number)).ToList();
            var outcome = room.Game.ProcessPlay(GetMyToken(room)!, cardList);

            if (!outcome.Success)
            {
                await Clients.Caller.SendAsync("ActionFailed", outcome.Error);
                return;
            }

            if (outcome.NeedsCardChoice)
            {
                await Clients.Caller.SendAsync("ChooseCardToTake",
                    room.Game.CardsAwaitingChoice.Select(c => new { Color = c.Color.ToString(), c.Number }));
                await BroadcastGameState(room, "StateUpdated");
                return;
            }

            await HandlePostActionState(room, outcome);
        }

        public async Task ChooseCard(CardDto card)
        {
            var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
            if (room?.Game == null) return;

            var chosenCard = new Models.Card(Enum.Parse<Models.CardColor>(card.Color), card.Number);
            var outcome = room.Game.ChooseCardToTake(GetMyToken(room)!, chosenCard);

            if (!outcome.Success)
            {
                await Clients.Caller.SendAsync("ActionFailed", outcome.Error);
                return;
            }

            await HandlePostActionState(room, outcome);
        }

        public async Task Pass()
        {
            var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
            if (room?.Game == null) return;

            var outcome = room.Game.ProcessPass(GetMyToken(room)!);

            if (!outcome.Success)
            {
                await Clients.Caller.SendAsync("ActionFailed", outcome.Error);
                return;
            }

            await HandlePostActionState(room, outcome);
        }

        private async Task HandlePostActionState(Models.GameRoom room, Models.PlayOutcome outcome)
        {
            if (outcome.RoundEnded)
            {
                await EndRound(room, outcome.PlayerEmptiedHand);
                return;
            }

            await BroadcastGameState(room, "StateUpdated");
        }

        private async Task BroadcastGameState(Models.GameRoom room, string eventName)
        {
            var game = room.Game!;

            foreach (var player in room.Players)
            {
                await Clients.Client(player.ConnectionId).SendAsync(eventName, new
                {
                    YourHand = player.Hand.Select(c => new { Color = c.Color.ToString(), c.Number }),
                    CurrentPlayerName = game.CurrentPlayer.Name,
                    CurrentCombination = game.CurrentCombination?.Cards.Select(c => new { Color = c.Color.ToString(), c.Number }),
                    CurrentValue = game.CurrentCombination?.Value,
                    AllPlayers = room.Players.Select(p => new { p.Name, CardCount = p.Hand.Count })
                });
            }
        }

        private async Task EndRound(Models.GameRoom room, bool playerEmptiedHand)
        {
            var game = room.Game!;

            foreach (var player in room.Players)
                player.TotalScore += player.Hand.Count;

            int threshold = 15 + 5 * (room.Players.Count - 3);
            bool gameOver = room.Players.Any(p => p.TotalScore >= threshold);

            if (gameOver)
            {
                var winner = room.Players.OrderBy(p => p.TotalScore).First();
                await Clients.Group(room.RoomCode).SendAsync("GameOver", new
                {
                    WinnerName = winner.Name,
                    Scores = room.Players.Select(p => new { p.Name, p.TotalScore })
                });

                room.IsGameStarted = false;
                room.Game = null;
                return;
            }

            string nextStarterToken = game.LastPlayerToPlayId ?? room.Players[0].PlayerToken;
            game.CurrentPlayerIndex = room.Players.FindIndex(p => p.PlayerToken == nextStarterToken);

            foreach (var player in room.Players)
            {
                await Clients.Client(player.ConnectionId).SendAsync("NewRoundStarted", new
                {
                    YourHand = player.Hand.Select(c => new { Color = c.Color.ToString(), c.Number }),
                    CurrentPlayerName = game.CurrentPlayer.Name,
                    Scores = room.Players.Select(p => new { p.Name, p.TotalScore }),
                    AllPlayers = room.Players.Select(p => new { p.Name, CardCount = p.Hand.Count })
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);

            if (room != null)
            {
                _roomService.RemovePlayer(Context.ConnectionId);

                if (_roomService.GetRoom(room.RoomCode) != null)
                {
                    await Clients.Group(room.RoomCode).SendAsync("PlayerListUpdated", GetPlayerNames(room));
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private List<string> GetPlayerNames(Models.GameRoom room)
        {
            return room.Players.Select(p => p.Name).ToList();
        }
    }
}