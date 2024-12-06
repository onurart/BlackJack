
using BlackJack.Helpers;
namespace BlackJack.Controllers;
[Route("api/blackjack")]
public class BlackJackController : ControllerBase
{
    private readonly SessionManager _sessionManager;
    private readonly Deck _deck;
    private readonly GameHelper _gameHelper;
    public BlackJackController(SessionManager sessionManager, GameHelper gameHelper)
    {
        _sessionManager = sessionManager;
        _gameHelper = gameHelper;
        _deck = new Deck();
    }
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        if (string.IsNullOrEmpty(loginRequest.username))
        {
            return BadRequest(new { Message = "The username field is required." });
        }

        var sessionId = _sessionManager.StartNewSession(loginRequest.username);
        return Ok(new { Message = "Login successful", SessionId = sessionId });
    }

    [HttpPost("new-game")]
    public IActionResult StartNewGame([FromBody] NewGameRequest request)
    {
        var validationResult = _gameHelper.ValidateSessionAndBet(request.SessionId, request.BetAmount);
        if (validationResult != null) return validationResult;

        var sessionId = Guid.Parse(request.SessionId);
        var gameSession = new GameSession(new CasualMode());
        gameSession.SetBetAmount(request.BetAmount);
        _sessionManager.UpdateGameSession(sessionId, gameSession);
        gameSession.StartNewSession();
        gameSession.StartGame(_deck);

        if (gameSession.GetPlayerScore() == 21)
        {
            return _gameHelper.EndGameWithBlackjack(gameSession, sessionId);
        }

        return Ok(new
        {
            Message = "Game started",
            SessionId = sessionId,
            BetAmount = request.BetAmount,
            PlayerHand = gameSession.GetPlayerHand(),
            DealerHand = gameSession.GetDealerHand(false),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerScore = gameSession.GetDealerScore(),
            IsGameOver = false
        });
    }

    [HttpPost("player-hit")]
    public IActionResult PlayerHit([FromBody] RequestSession request)
    {
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID" });
        }

        if (!gameSession.IsGameStarted)
        {
            return BadRequest(new { Message = "Game Over." });
        }

        if (gameSession.IsGameOver)
        {
            return Ok(new
            {
                Message = "Game Over",
            });
        }

        var result = gameSession.HitCurrentHand(_deck);

        if (gameSession.IsGameOver)
        {
            return Ok(new
            {
                Message = result,
                PlayerHands = gameSession.PlayerHand,
                PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
                Result = "Game Over"
            });
        }

        return Ok(new
        {
            Message = result,
            PlayerHands = gameSession.PlayerHand,
            PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
            CurrentHandIndex = gameSession.CurrentHandIndex + 1,
            Result = "Continue"
        });
    }

    [HttpPost("double-down")]
    public IActionResult DoubleDown([FromBody] RequestSession request)
    {
        if (request == null || string.IsNullOrEmpty(request.SessionId))
        {
            return BadRequest(new { Message = "Session ID is required." });
        }

        if (!Guid.TryParse(request.SessionId, out Guid sessionId))
        {
            return BadRequest(new { Message = "Invalid Session ID format." });
        }

        if (!_sessionManager.ValidateSession(sessionId))
        {
            return BadRequest(new { Message = "Invalid session ID" });
        }

        var gameSession = _sessionManager.GetGameSession(sessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Session not found." });
        }

        if (!gameSession.IsGameStarted)
        {
            return BadRequest(new { Message = "The game has not started. Please start the game first." });
        }

        if (gameSession.HasDoubledDown)
        {
            return BadRequest(new { Message = "You have already doubled down." });
        }

        gameSession.DoubleDown();
        var playerResult = gameSession.PlayerHit(_deck);
        if (playerResult == "Player Bust")
        {
            gameSession.EndGame();
            return Ok(new
            {
                Message = "You busted after doubling down. Game over.",
                PlayerHand = gameSession.GetPlayerHand(),
                PlayerScore = gameSession.GetPlayerScore(),
                Result = playerResult
            });
        }

        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        var dealerFinalScore = gameSession.GetDealerScore(true);
        var isPlayerWin = dealerFinalScore > 21 || gameSession.GetPlayerScore() > dealerFinalScore;
        var isDraw = gameSession.GetPlayerScore() == dealerFinalScore;
        var resultMessage = isPlayerWin
            ? "Player Wins!"
            : isDraw
                ? "Draw"
                : "Dealer Wins!";
        var payout = gameSession.CalculatePayout(isPlayerWin, isDraw);
        gameSession.EndGame();
        return Ok(new
        {
            Message = "Double down completed. " + resultMessage,
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = dealerFinalScore,
            BetAmount = payout,
            Result = resultMessage
        });
    }

    [HttpPost("hit-dealer")]
    public IActionResult HitDealer([FromBody] RequestSession request)
    {
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }

        if (gameSession.GetDealerScore(true) >= 17)
        {
            return Ok(new { Message = "Dealer cannot draw more cards." });
        }

        var newCard = _deck.DrawCard();
        gameSession.Dealer.Hand.Add(newCard);

        return Ok(new
        {
            NewCard = new { Rank = newCard.Rank, Suit = newCard.Suit, Value = newCard.Value },
            DealerScore = gameSession.GetDealerScore(true)
        });
    }

    [HttpPost("stay")]
    public IActionResult Stay([FromBody] RequestSession request)
    {
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }

        if (gameSession.IsGameOver)
        {
            return BadRequest(new { Message = "The game is already over. Please start a new game." });
        }

        var result = gameSession.StayCurrentHand(_deck);

        if (!gameSession.IsGameOver)
        {
            return Ok(new
            {
                Message = result,
                CurrentHandIndex = gameSession.CurrentHandIndex + 1,
                PlayerHands = gameSession.PlayerHand,
                PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
                Result = "Continue to the next hand"
            });
        }

        var dealerResult = _gameHelper.EndGameWithDealerPlay(gameSession);
        var handsResults = gameSession.PlayerHand.Select((hand, index) =>
        {
            var playerScore = CardHelper.CalculateHandScore(hand);
            var dealerScore = dealerResult.DealerScore;

            string resultMessage;
            if (playerScore > 21)
            {
                resultMessage = "Bust";
            }
            else if (dealerScore > 21 || playerScore > dealerScore)
            {
                resultMessage = "Win";
            }
            else if (playerScore == dealerScore)
            {
                resultMessage = "Draw";
            }
            else
            {
                resultMessage = "Lose";
            }

            return new
            {
                HandIndex = index + 1,
                PlayerScore = playerScore,
                Result = resultMessage
            };
        }).ToList();
        return Ok(new
        {
            Message = "Game Over",
            PlayerHands = gameSession.PlayerHand,
            HandsResults = handsResults,
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = dealerResult.DealerScore,
            BetAmount = dealerResult.BetAmount,
            OverallResult = dealerResult.Result
        });
    }
    
    [HttpPost("split")]
    public IActionResult Split([FromBody] RequestSession request)
    {
        // Geçerli bir oyun oturumu kontrolü
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }

        // Oyuncunun el bölme uygunluğu kontrolü
        if (!gameSession.CanSplit())
        {
            return BadRequest(new { Message = "Split is only available if both cards have the same value." });
        }

        // El bölme işlemi
        gameSession.Split(_deck);

        // Her iki elin skorlarını hesapla
        var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();

        // Her elin sonucunu belirle
        var handResults = gameSession.PlayerHand.Select((hand, index) =>
        {
            var score = CardHelper.CalculateHandScore(hand);
            if (score > 21)
            {
                return $"Hand {index + 1}: Bust";
            }
            return $"Hand {index + 1}: Continue";
        }).ToList();

        // Eğer herhangi bir el Bust olmuşsa oyunu sonlandır
        if (handScores.Any(score => score > 21))
        {
            gameSession.EndGame();
            return Ok(new
            {
                Message = "Player Bust. Game Over.",
                PlayerHands = gameSession.PlayerHand,
                PlayerScores = handScores,
                Results = handResults,
                OverallResult = "Bust"
            });
        }

        // Genel sonuç (örneğin "Beraber, Kazan")
        string overallResult = string.Join(", ", handResults);

        // Oyun devam ediyorsa
        return Ok(new
        {
            Message = "Hand split successfully. Playing Hand 1.",
            PlayerHands = gameSession.PlayerHand,
            PlayerScores = handScores,
            CurrentHandIndex = gameSession.CurrentHandIndex + 1,
            Results = handResults,
            OverallResult = overallResult
        });
    }

    // [HttpPost("split")]
    // public IActionResult Split([FromBody] RequestSession request)
    // {
    //     var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
    //     if (gameSession == null)
    //     {
    //         return BadRequest(new { Message = "Invalid session ID or game not started." });
    //     }
    //     if (!gameSession.CanSplit())
    //     {
    //         return BadRequest(new { Message = "Split is only available if both cards have the same value." });
    //     }
    //     gameSession.Split(_deck);
    //     var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
    //     if (handScores.Any(score => score > 21))
    //     {
    //         gameSession.EndGame();
    //         return Ok(new
    //         {
    //             Message = "Player Bust. Game Over.",
    //             PlayerHands = gameSession.PlayerHand,
    //             PlayerScores = handScores,
    //             Result = "Bust"
    //         });
    //     }
    //   
    //     return Ok(new
    //     {
    //         Message = "Hand split successfully. Playing Hand 1.",
    //         PlayerHands = gameSession.PlayerHand,
    //         PlayerScores = handScores,
    //         CurrentHandIndex = gameSession.CurrentHandIndex + 1,
    //         Result = "Continue"
    //     });
    // }

    // public IActionResult Split([FromBody] RequestSession request)
    // {
    //     var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
    //     if (gameSession == null)
    //     {
    //         return BadRequest(new { Message = "Invalid session ID or game not started." });
    //     }
    //
    //     if (!gameSession.CanSplit())
    //     {
    //         return BadRequest(new { Message = "Split is only available if both cards have the same value." });
    //     }
    //
    //     gameSession.Split(_deck);
    //
    //     return Ok(new
    //     {
    //         Message = "Hand split successfully. Playing Hand 1.",
    //         PlayerHands = gameSession.PlayerHand,
    //         PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
    //         CurrentHandIndex = gameSession.CurrentHandIndex + 1,
    //         Result = "Continue"
    //     });
    // }
}


// using BlackJack.Helpers;
// [ApiController]
// [Route("api/blackjack")]
// public class BlackJackController : ControllerBase
// {
//     private readonly SessionManager _sessionManager;
//     private readonly Deck _deck;
//     private readonly GameHelper _gameHelper;
//     public BlackJackController(SessionManager sessionManager, GameHelper gameHelper)
//     {
//         _sessionManager = sessionManager;
//         _gameHelper = gameHelper;
//         _deck = new Deck();
//     }
//     [HttpPost("login")]
//     public IActionResult Login([FromBody] LoginRequest loginRequest)
//     {
//         if (string.IsNullOrEmpty(loginRequest.username))
//         {
//             return BadRequest(new { Message = "The username field is required." });
//         }
//
//         var sessionId = _sessionManager.StartNewSession(loginRequest.username);
//         return Ok(new { Message = "Login successful", SessionId = sessionId });
//     }
//
//     [HttpPost("new-game")]
//     public IActionResult StartNewGame([FromBody] NewGameRequest request)
//     {
//         var validationResult = _gameHelper.ValidateSessionAndBet(request.SessionId, request.BetAmount);
//         if (validationResult != null) return validationResult;
//
//         var sessionId = Guid.Parse(request.SessionId);
//         var gameSession = new GameSession(new CasualMode());
//         gameSession.SetBetAmount(request.BetAmount);
//         _sessionManager.UpdateGameSession(sessionId, gameSession);
//         gameSession.StartNewSession();
//         gameSession.StartGame(_deck);
//
//         if (gameSession.GetPlayerScore() == 21)
//         {
//             return _gameHelper.EndGameWithBlackjack(gameSession, sessionId);
//         }
//
//         return Ok(new
//         {
//             Message = "Game started",
//             SessionId = sessionId,
//             BetAmount = request.BetAmount,
//             PlayerHand = gameSession.GetPlayerHand(),
//             DealerHand = gameSession.GetDealerHand(false),
//             PlayerScore = gameSession.GetPlayerScore(),
//             DealerScore = gameSession.GetDealerScore(),
//             IsGameOver = false
//         });
//     }
//
//
//     [HttpPost("player-hit")]
//     public IActionResult PlayerHit([FromBody] RequestSession request)
//     {
//         var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
//         if (gameSession == null)
//         {
//             return BadRequest(new { Message = "Invalid session ID" });
//         }
//         if (!gameSession.IsGameStarted)
//         {
//             return BadRequest(new { Message = "Game has not started." });
//         }
//         if (gameSession.IsGameOver)
//         {
//             return Ok(new { Message = "Game Over" });
//         }
//         var result = gameSession.HitCurrentHand(_deck);
//         if (gameSession.IsGameOver)
//         {
//             return Ok(new { Message = "Game Over" });
//         }
//           return Ok(new
//           {
//               Message = result,
//               PlayerHands = gameSession.PlayerHand,
//               PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//               CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//           Result = "Continue"
//              });
//     }
//
//     // [HttpPost("player-hit")]
//     // public IActionResult PlayerHit([FromBody] RequestSession request)
//     // {
//     //     var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
//     //     if (gameSession == null)
//     //     {
//     //         return BadRequest(new { Message = "Invalid session ID" });
//     //     }
//     //
//     //     if (!gameSession.IsGameStarted)
//     //     {
//     //         return BadRequest(new { Message = "Game Over." });
//     //     }
//     //
//     //     if (gameSession.IsGameOver)
//     //     {
//     //         return Ok(new
//     //         {
//     //             Message = "Game Over",
//     //             PlayerHands = gameSession.PlayerHand,
//     //             PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//     //             Result = "Game Over"
//     //         });
//     //     }
//     //
//     //     var result = gameSession.HitCurrentHand(_deck);
//     //
//     //     if (gameSession.IsGameOver)
//     //     {
//     //         return Ok(new
//     //         {
//     //             Message = result,
//     //             PlayerHands = gameSession.PlayerHand,
//     //             PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//     //             Result = "Game Over"
//     //         });
//     //     }
//     //
//     //     return Ok(new
//     //     {
//     //         Message = result,
//     //         PlayerHands = gameSession.PlayerHand,
//     //         PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//     //         CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//     //         Result = "Continue"
//     //     });
//     // }
//
//     [HttpPost("double-down")]
//     public IActionResult DoubleDown([FromBody] RequestSession request)
//     {
//         if (request == null || string.IsNullOrEmpty(request.SessionId))
//         {
//             return BadRequest(new { Message = "Session ID is required." });
//         }
//
//         if (!Guid.TryParse(request.SessionId, out Guid sessionId))
//         {
//             return BadRequest(new { Message = "Invalid Session ID format." });
//         }
//
//         if (!_sessionManager.ValidateSession(sessionId))
//         {
//             return BadRequest(new { Message = "Invalid session ID" });
//         }
//
//         var gameSession = _sessionManager.GetGameSession(sessionId);
//         if (gameSession == null)
//         {
//             return BadRequest(new { Message = "Session not found." });
//         }
//
//         if (!gameSession.IsGameStarted)
//         {
//             return BadRequest(new { Message = "The game has not started. Please start the game first." });
//         }
//
//         if (gameSession.HasDoubledDown)
//         {
//             return BadRequest(new { Message = "You have already doubled down." });
//         }
//
//         gameSession.DoubleDown();
//         var playerResult = gameSession.PlayerHit(_deck);
//         if (playerResult == "Player Bust")
//         {
//             gameSession.EndGame();
//             return Ok(new
//             {
//                 Message = "You busted after doubling down. Game over.",
//                 PlayerHand = gameSession.GetPlayerHand(),
//                 PlayerScore = gameSession.GetPlayerScore(),
//                 Result = playerResult
//             });
//         }
//
//         while (gameSession.GetDealerScore(true) < 17)
//         {
//             var dealerCard = _deck.DrawCard();
//             gameSession.Dealer.Hand.Add(dealerCard);
//         }
//
//         var dealerFinalScore = gameSession.GetDealerScore(true);
//         var isPlayerWin = dealerFinalScore > 21 || gameSession.GetPlayerScore() > dealerFinalScore;
//         var isDraw = gameSession.GetPlayerScore() == dealerFinalScore;
//         var resultMessage = isPlayerWin
//             ? "Player Wins!"
//             : isDraw
//                 ? "Draw"
//                 : "Dealer Wins!";
//         var payout = gameSession.CalculatePayout(isPlayerWin, isDraw);
//         gameSession.EndGame();
//         return Ok(new
//         {
//             Message = "Double down completed. " + resultMessage,
//             PlayerHand = gameSession.GetPlayerHand(),
//             PlayerScore = gameSession.GetPlayerScore(),
//             DealerHand = gameSession.GetDealerHand(true),
//             DealerScore = dealerFinalScore,
//             BetAmount = payout,
//             Result = resultMessage
//         });
//     }
//
//     [HttpPost("hit-dealer")]
//     public IActionResult HitDealer([FromBody] RequestSession request)
//     {
//         var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
//         if (gameSession == null)
//         {
//             return BadRequest(new { Message = "Invalid session ID or game not started." });
//         }
//
//         if (gameSession.GetDealerScore(true) >= 17)
//         {
//             return Ok(new { Message = "Dealer cannot draw more cards." });
//         }
//
//         var newCard = _deck.DrawCard();
//         gameSession.Dealer.Hand.Add(newCard);
//
//         return Ok(new
//         {
//             NewCard = new { Rank = newCard.Rank, Suit = newCard.Suit, Value = newCard.Value },
//             DealerScore = gameSession.GetDealerScore(true)
//         });
//     }
//
//     [HttpPost("stay")]
//     public IActionResult Stay([FromBody] RequestSession request)
//     {
//         var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
//         if (gameSession == null)
//         {
//             return BadRequest(new { Message = "Invalid session ID or game not started." });
//         }
//
//         if (gameSession.IsGameOver)
//         {
//             return BadRequest(new { Message = "The game is already over. Please start a new game." });
//         }
//
//         var result = gameSession.StayCurrentHand(_deck);
//
//         if (!gameSession.IsGameOver)
//         {
//             return Ok(new
//             {
//                 Message = result,
//                 CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//                 PlayerHands = gameSession.PlayerHand,
//                 PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//                 Result = "Continue to the next hand"
//             });
//         }
//
//         var dealerResult = _gameHelper.EndGameWithDealerPlay(gameSession);
//         var handsResults = gameSession.PlayerHand.Select((hand, index) =>
//         {
//             var playerScore = CardHelper.CalculateHandScore(hand);
//             var dealerScore = dealerResult.DealerScore;
//
//             string resultMessage;
//             if (playerScore > 21)
//             {
//                 resultMessage = "Bust";
//             }
//             else if (dealerScore > 21 || playerScore > dealerScore)
//             {
//                 resultMessage = "Win";
//             }
//             else if (playerScore == dealerScore)
//             {
//                 resultMessage = "Draw";
//             }
//             else
//             {
//                 resultMessage = "Lose";
//             }
//
//             return new
//             {
//                 HandIndex = index + 1,
//                 PlayerScore = playerScore,
//                 Result = resultMessage
//             };
//         }).ToList();
//         return Ok(new
//         {
//             Message = "Game Over",
//             PlayerHands = gameSession.PlayerHand,
//             HandsResults = handsResults,
//             DealerHand = gameSession.GetDealerHand(true),
//             DealerScore = dealerResult.DealerScore,
//             BetAmount = dealerResult.BetAmount,
//             OverallResult = dealerResult.Result
//         });
//     }
//
//     [HttpPost("split")]
//     public IActionResult Split([FromBody] RequestSession request)
//     {
//         var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
//         if (gameSession == null)
//         {
//             return BadRequest(new { Message = "Invalid session ID or game not started." });
//         }
//
//         if (!gameSession.CanSplit())
//         {
//             return BadRequest(new { Message = "Split is only available if both cards have the same value." });
//         }
//
//         gameSession.Split(_deck);
//
//         return Ok(new
//         {
//             Message = "Hand split successfully. Playing Hand 1.",
//             PlayerHands = gameSession.PlayerHand,
//             PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//             CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//             Result = "Continue"
//         });
//     }
// }