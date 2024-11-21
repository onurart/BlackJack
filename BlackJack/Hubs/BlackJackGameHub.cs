using BlackJack.Models;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;

namespace BlackJack.Hubs
{
    public class BlackJackGameHub : Hub
    {
        private readonly SessionManager _sessionManager;
        private readonly IValidator<BetRequest> _betValidator;

        public BlackJackGameHub(SessionManager sessionManager, IValidator<BetRequest> betValidator)
        {
            _sessionManager = sessionManager;
            _betValidator = betValidator;
        }

        public override async Task OnConnectedAsync()
        {
            // Bağlantı sırasında kullanıcı oturumları ilişkilendirilir
            Console.WriteLine($"User connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
            
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Bağlantı kesildiğinde kullanıcı oturumu temizlenir
            Console.WriteLine($"User disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task PlaceBet(BetRequest betRequest)
        {
            var validationResult = _betValidator.Validate(betRequest);

            if (!validationResult.IsValid)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new
                {
                    Message = "Validation failed",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
                return;
            }

            var sessionId = Context.Items["SessionId"] as Guid?;
            if (sessionId == null || !_sessionManager.ValidateSession(sessionId.Value))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Invalid session ID." });
                return;
            }

            var gameSession = _sessionManager.GetGameSession(sessionId.Value);
            gameSession.ValidateBet(betRequest.BetAmount);

            await Clients.Caller.SendAsync("BetPlaced", new { Message = "Bet placed", BetAmount = betRequest.BetAmount });
        }

        public async Task StartGame()
        {
            var sessionId = Context.Items["SessionId"] as Guid?;
            if (sessionId == null || !_sessionManager.ValidateSession(sessionId.Value))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Invalid session ID." });
                return;
            }

            var gameSession = _sessionManager.GetGameSession(sessionId.Value);
            gameSession.StartGame(new Deck());

            await Clients.All.SendAsync("GameStarted", new
            {
                PlayerHand = gameSession.GetPlayerHand(),
                DealerHand = gameSession.GetDealerHand(false),
                PlayerScore = gameSession.GetPlayerScore(),
                DealerScore = gameSession.GetDealerScore()
            });
        }
        
        public async Task PlayerHit()
        {
            var sessionId = Context.Items["SessionId"] as Guid?;
            if (sessionId == null || !_sessionManager.ValidateSession(sessionId.Value))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Invalid session ID." });
                return;
            }

            var gameSession = _sessionManager.GetGameSession(sessionId.Value);

            if (gameSession.IsGameOver)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "The game is over. You cannot perform any actions." });
                return;
            }

            var result = gameSession.PlayerHit(new Deck());
            await Clients.Caller.SendAsync("PlayerAction", new
            {
                PlayerHand = gameSession.GetPlayerHand(),
                Score = gameSession.GetPlayerScore(),
                ActionResult = result
            });

            if (result == "Player Bust")
            {
                await Clients.Caller.SendAsync("GameOver", "You lost! Dealer wins.");
            }
        }

        // public async Task PlayerHit()
        // {
        //     var sessionId = Context.Items["SessionId"] as Guid?;
        //     if (sessionId == null || !_sessionManager.ValidateSession(sessionId.Value))
        //     {
        //         await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Invalid session ID." });
        //         return;
        //     }
        //
        //     var gameSession = _sessionManager.GetGameSession(sessionId.Value);
        //
        //     if (!gameSession.IsGameStarted)
        //     {
        //         await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Game has not started yet." });
        //         return;
        //     }
        //
        //     var result = gameSession.PlayerHit(new Deck());
        //     await Clients.Caller.SendAsync("PlayerAction", new
        //     {
        //         PlayerHand = gameSession.GetPlayerHand(),
        //         Score = gameSession.GetPlayerScore(),
        //         ActionResult = result
        //     });
        //
        //     if (result == "Player Bust")
        //     {
        //         await Clients.Caller.SendAsync("GameOver", "Dealer Wins!");
        //     }
        // }

        public async Task PlayerStay()
        {
            var sessionId = Context.Items["SessionId"] as Guid?;
            if (sessionId == null || !_sessionManager.ValidateSession(sessionId.Value))
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Invalid session ID." });
                return;
            }

            var gameSession = _sessionManager.GetGameSession(sessionId.Value);

            if (!gameSession.IsGameStarted)
            {
                await Clients.Caller.SendAsync("ErrorMessage", new { Message = "Game has not started yet." });
                return;
            }

            var result = gameSession.PlayerStay(new Deck());
            await Clients.Caller.SendAsync("DealerAction", new
            {
                DealerHand = gameSession.GetDealerHand(true),
                DealerScore = gameSession.GetDealerScore(),
                Result = result
            });

            await Clients.Caller.SendAsync("GameOver", result);
        }
    }
}






// using BlackJack.Models;
// using FluentValidation;
// using Microsoft.AspNetCore.SignalR;
//
// namespace BlackJack.Hubs
// {
//   public class BlackJackGameHub : Hub
//     {
//         private readonly GameSession _gameSession;
//         private readonly Deck _deck;
//         private readonly IValidator<BetRequest> _betValidator;
//
//         public BlackJackGameHub(GameSession gameSession, IValidator<BetRequest> betValidator)
//         {
//             _gameSession = gameSession;
//             _deck = new Deck();
//             _betValidator = betValidator;
//         }
//
//         public async Task PlaceBet(BetRequest betRequest)
//         {
//             var validationResult = _betValidator.Validate(betRequest);
//
//             if (!validationResult.IsValid)
//             {
//                 await Clients.Caller.SendAsync("ErrorMessage", new
//                 {
//                     Message = "Validation failed",
//                     Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
//                 });
//                 return;
//             }
//
//             // Doğrulama başarılı
//             _gameSession.ValidateBet(betRequest.BetAmount); // FluentValidation doğrulamasından sonra çalıştırılabilir
//             await Clients.Caller.SendAsync("BetPlaced", new { Message = "Bet placed", BetAmount = betRequest.BetAmount });
//         }
//
//         public async Task StartGame()
//         {
//             _gameSession.StartGame(_deck);
//             await Clients.All.SendAsync("GameStarted", new
//             {
//                 PlayerHand = _gameSession.GetPlayerHand(),
//                 DealerHand = _gameSession.GetDealerHand(false),
//                 PlayerScore = _gameSession.GetPlayerScore(),
//                 DealerScore = _gameSession.GetDealerScore()
//             });
//         }
//
//         public async Task PlayerHit()
//         {
//             var result = _gameSession.PlayerHit(_deck);
//             await Clients.Caller.SendAsync("PlayerAction", new
//             {
//                 PlayerHand = _gameSession.GetPlayerHand(),
//                 Score = _gameSession.GetPlayerScore(),
//                 ActionResult = result
//             });
//
//             if (result == "Player Bust")
//             {
//                 await Clients.Caller.SendAsync("GameOver", "Dealer Wins!");
//             }
//         }
//
//         public async Task PlayerStay()
//         {
//             var result = _gameSession.PlayerStay(_deck);
//             await Clients.Caller.SendAsync("DealerAction", new
//             {
//                 DealerHand = _gameSession.GetDealerHand(true),
//                 DealerScore = _gameSession.GetDealerScore(),
//                 Result = result
//             });
//             await Clients.Caller.SendAsync("GameOver", result);
//         }
//     }
// }