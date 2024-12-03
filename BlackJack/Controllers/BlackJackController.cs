using BlackJack.Helpers;

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
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }
        if (!gameSession.IsGameStarted)
        {
            return BadRequest(new { Message = "The game has not started. Please start the game first." });
        }
        if (gameSession.IsGameOver)
        {
            return BadRequest(new { Message = "The game is already over. Please start a new game." });
        }
        try
        {
            var result = gameSession.HitCurrentHand(_deck);
            if (gameSession.IsGameOver)
            {
                return _gameHelper.EndGameWithPlayerResult(gameSession, result);
            }
            return Ok(new
            {
                Message = result,
                PlayerHands = gameSession.PlayerHands,
                PlayerScore = gameSession.GetPlayerScore(),
                CurrentHandIndex = gameSession.CurrentHandIndex + 1
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = "An error occurred during the Player Hit action.",
                Details = ex.Message
            });
        }
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
    if (playerResult == "Player Bust") {
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
            gameSession.NextHand();
            return Ok(new
            {
                PlayerHand = gameSession.GetPlayerHand(),
                PlayerScore = gameSession.GetPlayerScore(),
                DealerHand = new List<Card>(), 
                DealerScore = (int?)null,
                BetAmount = (decimal?)null,
                Result = "Continue to the next hand"
            });
        }
        var dealerResult = _gameHelper.EndGameWithDealerPlay(gameSession);
        return Ok(new
        {
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = dealerResult.DealerScore,
            BetAmount = dealerResult.BetAmount,
            Result = dealerResult.Result
        });
    }


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
    //         gameSession.NextHand();
    //         return Ok(new
    //         {
    //             Message = $"Stayed on Hand {gameSession.CurrentHandIndex}. Moving to Hand {gameSession.CurrentHandIndex + 1}.",
    //             PlayerHands = gameSession.PlayerHands,
    //             PlayerScore = gameSession.GetPlayerScore(),
    //             CurrentHandIndex = gameSession.CurrentHandIndex + 1
    //         });
    //     }
    //
    //     var dealerResult = _gameHelper.EndGameWithDealerPlay(gameSession);
    //
    //     return Ok(dealerResult);
    // }

    
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
    //             return BadRequest(new { Message = "All hands have been completed." });
    //         }
    //
    //         var result = gameSession.StayCurrentHand(_deck);
    //
    //         if (!gameSession.IsGameOver)
    //         {
    //             gameSession.NextHand();
    //             return Ok(new
    //             {
    //                 Message =
    //                     $"Stayed on Hand {gameSession.CurrentHandIndex}. Moving to Hand {gameSession.CurrentHandIndex + 1}.",
    //                 PlayerHands = gameSession.PlayerHands,
    //                 PlayerScore = gameSession.GetPlayerScore(),
    //                 CurrentHandIndex = gameSession.CurrentHandIndex + 1
    //             });
    //         }
    //
    //     return Ok(new
    //     {
    //         Message = "Game over.",
    //         PlayerHands = gameSession.PlayerHands,
    //         PlayerScore = gameSession.GetPlayerScore(),
    //         DealerHand = gameSession.GetDealerHand(true),
    //         DealerScore = gameSession.GetDealerScore(true),
    //         Result = result
    //     });
    // }

    [HttpPost("split")]
    public IActionResult Split([FromBody] RequestSession request)
    {
        var gameSession = _gameHelper.GetValidatedGameSession(request.SessionId);
        if (gameSession == null)
        {
            return BadRequest(new { Message = "Invalid session ID or game not started." });
        }

        if (!gameSession.CanSplit())
        {
            return BadRequest(new { Message = "Split is only available if both cards have the same value." });
        }

        gameSession.Split(_deck);

        return Ok(new
        {
            Message = "Hand split successfully. Playing Hand 1.",
            PlayerHands = gameSession.PlayerHands,
            PlayerScores = gameSession.PlayerHands.Select(CardHelper.CalculateHandScore).ToList(), // Skor hesaplama
            CurrentHandIndex = gameSession.CurrentHandIndex + 1
        });
    }

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
    //         PlayerHands = gameSession.PlayerHands,
    //         CurrentHandIndex = gameSession.CurrentHandIndex + 1
    //     });
    // }

}