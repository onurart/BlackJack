namespace BlackJack.Controllers;

[ApiController]
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
                PlayerHands = gameSession.PlayerHand,
                PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
                HandResults = gameSession.PlayerHand.Select((hand, index) => $"Hand {index + 1}: Game Over").ToList()
            });
        }

        var result = gameSession.HitCurrentHand(_deck);
        var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
        bool showDealerHand = handScores.All(score => score > 21) || handScores.Any(score => score <= 21);
        var dealerHand = gameSession.GetDealerHand(showDealerHand);
        var dealerScore = gameSession.GetDealerScore(showDealerHand);
        var handResults = gameSession.PlayerHand.Select((hand, index) =>
        {
            var score = CardHelper.CalculateHandScore(hand);
            if (score > 21)
            {
                return $"Hand {index + 1}: Lose";
            }

            if (dealerScore > 21 || score > dealerScore)
            {
                return $"Hand {index + 1}: Player Wins!";
            }

            if (score == dealerScore)
            {
                return $"Hand {index + 1}: Draw";
            }

            return $"Hand {index + 1}: Lose";
        }).ToList();
        if (gameSession.IsGameOver)
        {
            return Ok(new
            {
                Message = result,
                PlayerHands = gameSession.PlayerHand,
                PlayerScores = handScores,
                DealerHand = dealerHand,
                DealerScore = dealerScore,
                HandResults = handResults,
                OverallResult = string.Join(", ", handResults)
            });
        }

        return Ok(new
        {
            Message = result,
            PlayerHands = gameSession.PlayerHand,
            PlayerScores = handScores,
            DealerHand = dealerHand,
            DealerScore = dealerScore,
            HandResults = handResults,
            CurrentHandIndex = gameSession.CurrentHandIndex + 1,
            OverallResult = "Continue"
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
        return BadRequest(new { Message = "You have already doubled down on this hand." });
    }

    // Perform double-down for the current hand
    gameSession.DoubleDown();
    var playerResult = gameSession.PlayerHit(_deck);

    // Check for bust (score > 21) on the current hand
    var currentHandScore = CardHelper.CalculateHandScore(gameSession.PlayerHand[gameSession.CurrentHandIndex]);
    if (currentHandScore > 21)
    {
        if (gameSession.CurrentHandIndex < gameSession.PlayerHand.Count - 1)
        {
            gameSession.NextHand();
            return Ok(new
            {
                Message = "You lost on the current hand. Moving to the next hand.",
                PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
                PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
                CurrentHandIndex = gameSession.CurrentHandIndex + 1,
                OverallResult = "Lose"
            });
        }
        else
        {
            gameSession.EndGame();
            return Ok(new
            {
                Message = "You lose after doubling down. Game over.",
                PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
                PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
                CurrentHandIndex = gameSession.CurrentHandIndex + 1,
                OverallResult = "Lose"
            });
        }
    }

    // If the dealer reveals cards after the final hand
    if (gameSession.CurrentHandIndex == gameSession.PlayerHand.Count - 1)
    {
        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        var dealerFinalScore = gameSession.GetDealerScore(true);
        var handResults = gameSession.PlayerHand.Select((hand, index) =>
        {
            var score = CardHelper.CalculateHandScore(hand);
            if (score > 21)
            {
                return $"Hand {index + 1}: Lose";
            }
            if (dealerFinalScore > 21 || score > dealerFinalScore)
            {
                return $"Hand {index + 1}: Player Wins!";
            }
            if (score == dealerFinalScore)
            {
                return $"Hand {index + 1}: Draw";
            }
            return $"Hand {index + 1}: Dealer Wins!";
        }).ToList();

        gameSession.EndGame();

        return Ok(new
        {
            Message = "Double down completed.",
            PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
            PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = dealerFinalScore,
            HandResults = handResults,
            OverallResult = string.Join(", ", handResults)
        });
    }

    // Game continues to the next hand
    gameSession.NextHand();
    return Ok(new
    {
        Message = "Double down completed. Moving to the next hand.",
        PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
        PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
        CurrentHandIndex = gameSession.CurrentHandIndex + 1,
        OverallResult = "Continue"
    });
}


//     [HttpPost("double-down")]
// public IActionResult DoubleDown([FromBody] RequestSession request)
// {
//     if (request == null || string.IsNullOrEmpty(request.SessionId))
//     {
//         return BadRequest(new { Message = "Session ID is required." });
//     }
//
//     if (!Guid.TryParse(request.SessionId, out Guid sessionId))
//     {
//         return BadRequest(new { Message = "Invalid Session ID format." });
//     }
//
//     if (!_sessionManager.ValidateSession(sessionId))
//     {
//         return BadRequest(new { Message = "Invalid session ID" });
//     }
//
//     var gameSession = _sessionManager.GetGameSession(sessionId);
//     if (gameSession == null)
//     {
//         return BadRequest(new { Message = "Session not found." });
//     }
//
//     if (!gameSession.IsGameStarted)
//     {
//         return BadRequest(new { Message = "The game has not started. Please start the game first." });
//     }
//
//     if (gameSession.HasDoubledDown)
//     {
//         return BadRequest(new { Message = "You have already doubled down on this hand." });
//     }
//
//     gameSession.DoubleDown();
//     var playerResult = gameSession.PlayerHit(_deck);
//
//     var currentHandScore = CardHelper.CalculateHandScore(gameSession.PlayerHand[gameSession.CurrentHandIndex]);
//     if (currentHandScore > 21)
//     {
//         if (gameSession.CurrentHandIndex < gameSession.PlayerHand.Count - 1)
//         {
//             gameSession.NextHand();
//             return Ok(new
//             {
//                 Message = "You lost on the current hand. Moving to the next hand.",
//                 PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
//                 PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//                 CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//                 OverallResult = "Lose"
//             });
//         }
//         else
//         {
//             gameSession.EndGame();
//             return Ok(new
//             {
//                 Message = "You lose after doubling down. Game over.",
//                 PlayerHands = gameSession.PlayerHand,
//                 PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//                 OverallResult = "Lose"
//             });
//         }
//     }
//
//     if (gameSession.CurrentHandIndex == gameSession.PlayerHand.Count - 1)
//     {
//         while (gameSession.GetDealerScore(true) < 17)
//         {
//             var dealerCard = _deck.DrawCard();
//             gameSession.Dealer.Hand.Add(dealerCard);
//         }
//
//         var dealerFinalScore = gameSession.GetDealerScore(true);
//         var handResults = gameSession.PlayerHand.Select((hand, index) =>
//         {
//             var score = CardHelper.CalculateHandScore(hand);
//             if (score > 21)
//             {
//                 return $"Hand {index + 1}: Lose";
//             }
//             if (dealerFinalScore > 21 || score > dealerFinalScore)
//             {
//                 return $"Hand {index + 1}: Player Wins!";
//             }
//             if (score == dealerFinalScore)
//             {
//                 return $"Hand {index + 1}: Draw";
//             }
//             return $"Hand {index + 1}: Dealer Wins!";
//         }).ToList();
//
//         gameSession.EndGame();
//
//         return Ok(new
//         {
//             Message = "Double down completed.",
//             PlayerHands = gameSession.PlayerHand,
//             PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//             DealerHand = gameSession.GetDealerHand(true),
//             DealerScore = dealerFinalScore,
//             HandResults = handResults,
//             OverallResult = string.Join(", ", handResults)
//         });
//     }
//
//     // If the game continues to the next hand
//     gameSession.NextHand();
//     return Ok(new
//     {
//         Message = "Double down completed. Moving to the next hand.",
//         PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
//         PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
//         CurrentHandIndex = gameSession.CurrentHandIndex + 1,
//         OverallResult = "Continue"
//     });
// }




    // [HttpPost("double-down")]
    // public IActionResult DoubleDown([FromBody] RequestSession request)
    // {
    //     if (request == null || string.IsNullOrEmpty(request.SessionId))
    //     {
    //         return BadRequest(new { Message = "Session ID is required." });
    //     }
    //
    //     if (!Guid.TryParse(request.SessionId, out Guid sessionId))
    //     {
    //         return BadRequest(new { Message = "Invalid Session ID format." });
    //     }
    //
    //     if (!_sessionManager.ValidateSession(sessionId))
    //     {
    //         return BadRequest(new { Message = "Invalid session ID" });
    //     }
    //
    //     var gameSession = _sessionManager.GetGameSession(sessionId);
    //     if (gameSession == null)
    //     {
    //         return BadRequest(new { Message = "Session not found." });
    //     }
    //
    //     if (!gameSession.IsGameStarted)
    //     {
    //         return BadRequest(new { Message = "The game has not started. Please start the game first." });
    //     }
    //
    //     if (gameSession.HasDoubledDown)
    //     {
    //         return BadRequest(new { Message = "You have already doubled down." });
    //     }
    //
    //     gameSession.DoubleDown();
    //     var playerResult = gameSession.PlayerHit(_deck);
    //     if (playerResult == "Player lose")
    //     {
    //         gameSession.EndGame();
    //         return Ok(new
    //         {
    //             Message = "You lose after doubling down. Game over.",
    //             PlayerHand = gameSession.GetPlayerHand(),
    //             PlayerScore = gameSession.GetPlayerScore(),
    //             Result = playerResult
    //         });
    //     }
    //
    //     while (gameSession.GetDealerScore(true) < 17)
    //     {
    //         var dealerCard = _deck.DrawCard();
    //         gameSession.Dealer.Hand.Add(dealerCard);
    //     }
    //
    //     var dealerFinalScore = gameSession.GetDealerScore(true);
    //     var isPlayerWin = dealerFinalScore > 21 || gameSession.GetPlayerScore() > dealerFinalScore;
    //     var isDraw = gameSession.GetPlayerScore() == dealerFinalScore;
    //     var resultMessage = isPlayerWin
    //         ? "Player Wins!"
    //         : isDraw
    //             ? "Draw"
    //             : "Dealer Wins!";
    //     var payout = gameSession.CalculatePayout(isPlayerWin, isDraw);
    //     gameSession.EndGame();
    //     return Ok(new
    //     {
    //         Message = "Double down completed. " + resultMessage,
    //         PlayerHand = gameSession.GetPlayerHand(),
    //         PlayerScore = gameSession.GetPlayerScore(),
    //         DealerHand = gameSession.GetDealerHand(true),
    //         DealerScore = dealerFinalScore,
    //         BetAmount = payout,
    //         Result = resultMessage
    //     });
    // }

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

    // Perform the stay operation for the current hand
    var result = gameSession.StayCurrentHand(_deck);

    // Check if the game continues to the next hand
    if (!gameSession.IsGameOver)
    {
        return Ok(new
        {
            Message = result,
            PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
            PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
            CurrentHandIndex = gameSession.CurrentHandIndex + 1,
            OverallResult = "Continue to the next hand"
        });
    }

    // Calculate scores for the final state
    var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
    var dealerScore = gameSession.GetDealerScore(true);
    var dealerHand = gameSession.GetDealerHand(true);

    // Evaluate results for each hand
    var handResults = gameSession.PlayerHand.Select((hand, index) =>
    {
        var playerScore = CardHelper.CalculateHandScore(hand);
        if (playerScore > 21)
        {
            return $"Hand {index + 1}: Lose";
        }
        if (dealerScore > 21 || playerScore > dealerScore)
        {
            return $"Hand {index + 1}: Win";
        }
        if (playerScore == dealerScore)
        {
            return $"Hand {index + 1}: Draw";
        }
        return $"Hand {index + 1}: Lose";
    }).ToList();

    gameSession.EndGame();

    // Return final game results
    return Ok(new
    {
        Message = "Game Over",
        PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
        PlayerScores = handScores,
        DealerHand = dealerHand,
        DealerScore = dealerScore,
        HandResults = handResults,
        OverallResult = string.Join(", ", handResults)
    });
}

    
    // [HttpPost("stay")]
    // public IActionResult Stay([FromBody] RequestSession request)
    // {
    //     var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
    //     if (gameSession == null)
    //     {
    //         return BadRequest(new { Message = "Invalid session ID or game not started." });
    //     }
    //
    //     if (gameSession.IsGameOver)
    //     {
    //         return BadRequest(new { Message = "The game is already over. Please start a new game." });
    //     }
    //
    //     var result = gameSession.StayCurrentHand(_deck);
    //
    //     if (!gameSession.IsGameOver)
    //     {
    //         return Ok(new
    //         {
    //             Message = result,
    //             CurrentHandIndex = gameSession.CurrentHandIndex + 1,
    //             PlayerHands = gameSession.PlayerHand,
    //             PlayerScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList(),
    //             Result = "Continue to the next hand"
    //         });
    //     }
    //
    //     var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
    //     var dealerScore = gameSession.GetDealerScore(true);
    //     var dealerHand = gameSession.GetDealerHand(true);
    //     var handsResults = gameSession.PlayerHand.Select((hand, index) =>
    //     {
    //         var playerScore = CardHelper.CalculateHandScore(hand);
    //         string resultMessage;
    //         if (playerScore > 21)
    //         {
    //             resultMessage = $"Hand {index + 1}: Lose";
    //         }
    //
    //         else if (dealerScore > 21 || playerScore > dealerScore)
    //         {
    //             resultMessage = $"Hand {index + 1}: Win";
    //         }
    //
    //         else if (playerScore == dealerScore)
    //         {
    //             resultMessage = $"Hand {index + 1}: Draw";
    //         }
    //         else
    //         {
    //             resultMessage = $"Hand {index + 1}: Lose";
    //         }
    //
    //         return new
    //         {
    //             HandIndex = index + 1,
    //             PlayerScore = playerScore,
    //             Result = resultMessage
    //         };
    //     }).ToList();
    //     var overallResult = handsResults.Select(r => r.Result).ToList();
    //     gameSession.EndGame();
    //     return Ok(new
    //     {
    //         Message = "Game Over",
    //         PlayerHands = gameSession.PlayerHand,
    //         PlayerScores = handScores,
    //         DealerHand = dealerHand,
    //         DealerScore = dealerScore,
    //         HandsResults = handsResults,
    //         OverallResult = overallResult
    //     });
    // }

  
    [HttpPost("split")]
    public IActionResult Split([FromBody] RequestSession request)
    {
        // Validate game session
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }

        // Check if splitting is allowed
        if (!gameSession.CanSplit())
        {
            return BadRequest(new { Message = "Split is only available if both cards have the same value." });
        }

        // Perform the split operation
        gameSession.Split(_deck);

        // Calculate scores for player hands
        var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();

        // Return a response without dealer information
        return Ok(new
        {
            Message = "Hand split successfully. Playing Hand 1.",
            PlayerHands = gameSession.PlayerHand.Select((hand, index) => new { HandIndex = index + 1, Cards = hand }).ToList(),
            PlayerScores = handScores,
            CurrentHandIndex = gameSession.CurrentHandIndex + 1,
            OverallResult = "Continue"
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
    //
    //     if (!gameSession.CanSplit())
    //     {
    //         return BadRequest(new { Message = "Split is only available if both cards have the same value." });
    //     }
    //
    //     gameSession.Split(_deck);
    //
    //     var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
    //     bool showDealerHand = handScores.All(score => score > 21) || handScores.Any(score => score <= 21);
    //     var dealerHand = gameSession.GetDealerHand(showDealerHand);
    //     int? dealerScore = showDealerHand ? gameSession.GetDealerScore(true) : (int?)null;
    //
    //     var handResults = gameSession.PlayerHand.Select((hand, index) =>
    //     {
    //         var score = CardHelper.CalculateHandScore(hand);
    //         if (score > 21)
    //         {
    //             return $"Hand {index + 1}: Lose";
    //         }
    //
    //         if (dealerScore > 21 || score > dealerScore)
    //         {
    //             return $"Hand {index + 1}: Player Wins!";
    //         }
    //
    //         if (score == dealerScore)
    //         {
    //             return $"Hand {index + 1}: Draw";
    //         }
    //
    //         return $"Hand {index + 1}: Lose";
    //     }).ToList();
    //
    //     if (handScores.All(score => score > 21))
    //     {
    //         gameSession.EndGame();
    //         return Ok(new
    //         {
    //             Message = "Both hands lose. Dealer wins.",
    //             PlayerHands = gameSession.PlayerHand,
    //             PlayerScores = handScores,
    //             DealerHand = dealerHand,
    //             DealerScore = dealerScore,
    //             HandResults = handResults,
    //             OverallResult = string.Join(", ", handResults)
    //         });
    //     }
    //
    //     return Ok(new
    //     {
    //         Message = "Hand split successfully. Playing Hand 1.",
    //         PlayerHands = gameSession.PlayerHand,
    //         PlayerScores = handScores,
    //         DealerHand = dealerHand,
    //         DealerScore = dealerScore,
    //         HandResults = handResults,
    //         OverallResult = string.Join(", ", handResults)
    //     });
    // }

    // [HttpPost("split")]
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
    //     var handScores = gameSession.PlayerHand.Select(CardHelper.CalculateHandScore).ToList();
    //     bool showDealerHand = handScores.All(score => score > 21) || handScores.Any(score => score <= 21);
    //     var dealerHand = gameSession.GetDealerHand(showDealerHand);
    //     var dealerScore = gameSession.GetDealerScore(showDealerHand);
    //     var handResults = gameSession.PlayerHand.Select((hand, index) =>
    //     {
    //         var score = CardHelper.CalculateHandScore(hand);
    //         if (score > 21)
    //         {
    //             return $"Hand {index + 1}: Lose";
    //         }
    //
    //         if (dealerScore > 21 || score > dealerScore)
    //         {
    //             return $"Hand {index + 1}: Player Wins!";
    //         }
    //
    //         if (score == dealerScore)
    //         {
    //             return $"Hand {index + 1}: Draw";
    //         }
    //
    //         return $"Hand {index + 1}: Lose";
    //     }).ToList();
    //     if (handScores.All(score => score > 21))
    //     {
    //         gameSession.EndGame();
    //         return Ok(new
    //         {
    //             Message = "Both hands lose. Dealer wins.",
    //             PlayerHands = gameSession.PlayerHand,
    //             PlayerScores = handScores,
    //             DealerHand = dealerHand,
    //             DealerScore = dealerScore,
    //             HandResults = handResults,
    //             OverallResult = string.Join(", ", handResults)
    //         });
    //     }
    //
    //     return Ok(new
    //     {
    //         Message = "Hand split successfully. Playing Hand 1.",
    //         PlayerHands = gameSession.PlayerHand,
    //         PlayerScores = handScores,
    //         DealerHand = dealerHand,
    //         DealerScore = dealerScore,
    //         HandResults = handResults,
    //         OverallResult = string.Join(", ", handResults)
    //     });
    // }
}