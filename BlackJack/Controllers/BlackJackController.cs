[ApiController]
[Route("api/blackjack")]
public class BlackJackController : ControllerBase
{
    private readonly SessionManager _sessionManager;
    private readonly Deck _deck;

    public BlackJackController(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _deck = new Deck();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
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

    if (request.BetAmount <= 0)
    {
        return BadRequest(new { Message = "Bet amount must be greater than zero." });
    }

    // Yeni oyun oturumu oluştur
    var gameSession = new GameSession(new CasualMode());
    gameSession.SetBetAmount(request.BetAmount);
    _sessionManager.UpdateGameSession(sessionId, gameSession);

    // Oturumu sıfırla ve oyunu başlat
    gameSession.StartNewSession();
    gameSession.StartGame(_deck);

    // Oyuncunun başlangıçta 21 puanına ulaşıp ulaşmadığını kontrol et
    if (gameSession.GetPlayerScore() == 21)
    {
        // Krupiyenin hamlelerini yap
        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        // Oyunu bitir ve sonucu belirle
        var dealerScore = gameSession.GetDealerScore(true);
        var result = dealerScore == 21
            ? "Dealer also reached 21! It's a draw."
            : "Player Wins with Blackjack!";

        // Oyunu sona erdir
        gameSession.EndGame(); // Yeni bir EndGame metodu yazılabilir

        return Ok(new
        {
            Message = "Player reached 21. Game over.",
            SessionId = sessionId,
            BetAmount = request.BetAmount,
            PlayerHand = gameSession.GetPlayerHand(),
            DealerHand = gameSession.GetDealerHand(true),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerScore = dealerScore,
            Result = result,
            IsGameOver = true
        });
    }

    // Normal oyun başlangıcı
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

        var result = gameSession.PlayerHit(_deck);

        return Ok(new
        {
            Message = "Double down completed.",
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            BetAmount = gameSession.GetBetAmount(),
            Result = result
        });
    }



    // [HttpPost("new-game")]
// public IActionResult StartNewGame([FromBody] NewGameRequest request)
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
//     if (request.BetAmount <= 0)
//     {
//         return BadRequest(new { Message = "Bet amount must be greater than zero." });
//     }
//
//     // Yeni oyun oturumu oluştur
//     var gameSession = new GameSession(new CasualMode());
//     gameSession.SetBetAmount(request.BetAmount);
//     _sessionManager.UpdateGameSession(sessionId, gameSession);
//
//     // Oturum sıfırla ve oyunu başlat
//     gameSession.StartNewSession();
//     gameSession.StartGame(_deck);
//
//     // Oyuncu 21'e ulaştıysa
//     if (gameSession.GetPlayerScore() == 21)
//     {
//         // Krupiyenin hamlelerini yap
//         while (gameSession.GetDealerScore(true) < 17)
//         {
//             var dealerCard = _deck.DrawCard();
//             gameSession.Dealer.Hand.Add(dealerCard);
//         }
//
//         var dealerScore = gameSession.GetDealerScore(true);
//         var result = dealerScore == 21
//             ? "Dealer also reached 21! It's a draw."
//             : "Player Wins!";
//
//         // Oyuncunun hamlesini bitir ve 21'e ulaşmasını belirt
//         return Ok(new
//         {
//             Message = "Player reached 21. Dealer's turn completed.",
//             SessionId = sessionId,
//             BetAmount = request.BetAmount,
//             PlayerHand = gameSession.GetPlayerHand(),
//             DealerHand = gameSession.GetDealerHand(true),
//             PlayerScore = gameSession.GetPlayerScore(),
//             DealerScore = dealerScore,
//             Result = result,
//             IsGameOver = true // Oyun sona erdiğini belirt
//         });
//     }
//
//     // Normal oyun başlangıcı
//     return Ok(new
//     {
//         Message = "Game started",
//         SessionId = sessionId,
//         BetAmount = request.BetAmount,
//         PlayerHand = gameSession.GetPlayerHand(),
//         DealerHand = gameSession.GetDealerHand(false),
//         PlayerScore = gameSession.GetPlayerScore(),
//         DealerScore = gameSession.GetDealerScore(),
//         IsGameOver = false // Oyun devam ediyor
//     });
// }

    //    [HttpPost("new-game")]
//     public IActionResult StartNewGame([FromBody] NewGameRequest request)
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
//     if (request.BetAmount <= 0)
//     {
//         return BadRequest(new { Message = "Bet amount must be greater than zero." });
//     }
//
//     var gameSession = new GameSession(new CasualMode());
//     gameSession.SetBetAmount(request.BetAmount);
//     _sessionManager.UpdateGameSession(sessionId, gameSession);
//
//     gameSession.StartNewSession();
//     gameSession.StartGame(_deck);
//
//     if (gameSession.GetPlayerScore() == 21)
//     {
//         while (gameSession.GetDealerScore(true) < 17)
//         {
//             var dealerCard = _deck.DrawCard();
//             gameSession.Dealer.Hand.Add(dealerCard);
//         }
//
//         var dealerScore = gameSession.GetDealerScore(true);
//         var result = dealerScore == 21
//             ? "Dealer also reached 21! It's a draw."
//             : "Player Wins!";
//
//         return Ok(new
//         {
//             Message = "Player reached 21. Dealer's turn completed.",
//             SessionId = sessionId,
//             BetAmount = request.BetAmount,
//             PlayerHand = gameSession.GetPlayerHand(),
//             DealerHand = gameSession.GetDealerHand(true),
//             PlayerScore = gameSession.GetPlayerScore(),
//             DealerScore = dealerScore,
//             Result = result
//         });
//     }
//
//     return Ok(new
//     {
//         Message = "Game started",
//         SessionId = sessionId,
//         BetAmount = request.BetAmount,
//         PlayerHand = gameSession.GetPlayerHand(),
//         DealerHand = gameSession.GetDealerHand(false),
//         PlayerScore = gameSession.GetPlayerScore(),
//         DealerScore = gameSession.GetDealerScore()
//     });
// }
    [HttpPost("player-hit")]
public IActionResult PlayerHit([FromBody] RequestSession request)
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

    // Oyuncu 21 puanına ulaşmışsa kart çekmesine izin verme
    if (gameSession.GetPlayerScore() == 21)
    {
        return BadRequest(new { Message = "You already have 21 points. You cannot draw more cards." });
    }

    var result = gameSession.PlayerHit(_deck);

    // Oyuncu 21'e ulaştıysa krupiye hamlelerini yap
    if (result == "Player Wins with 21!")
    {
        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        var dealerScore = gameSession.GetDealerScore(true);
        var finalResult = dealerScore == 21
            ? "Dealer also reached 21! It's a draw."
            : "Player Wins!";

        return Ok(new
        {
            Message = "Player reached 21. Dealer's turn completed.",
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = dealerScore,
            Result = finalResult
        });
    }

    // Oyuncu başarısız olduysa
    if (result == "Player Bust")
    {
        return Ok(new
        {
            Message = "Player bust. Dealer wins.",
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = gameSession.GetDealerScore(true),
            Result = "Dealer Wins!"
        });
    }

    // Oyuncunun hamlesi devam ediyorsa
    return Ok(new
    {
        PlayerHand = gameSession.GetPlayerHand(),
        PlayerScore = gameSession.GetPlayerScore(),
        Result = result
    });
}


    // [HttpPost("new-game")]
        // public IActionResult StartNewGame([FromBody] NewGameRequest request)
        // {
        //     // Request geçerliliğini kontrol et
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
        //     if (request.BetAmount <= 0)
        //     {
        //         return BadRequest(new { Message = "Bet amount must be greater than zero." });
        //     }
        //
        //     // Yeni oyun oturumu oluştur
        //     var gameSession = new GameSession(new CasualMode());
        //     gameSession.SetBetAmount(request.BetAmount);
        //     _sessionManager.UpdateGameSession(sessionId, gameSession);
        //
        //     // Oturum sıfırla ve oyunu başlat
        //     gameSession.StartNewSession();
        //     gameSession.StartGame(_deck);
        //
        //     // Oyuncunun başlangıçta 21 puanına ulaşıp ulaşmadığını kontrol et
        //     if (gameSession.GetPlayerScore() == 21)
        //     {
        //         // Krupiyenin hamlelerini yap
        //         while (gameSession.GetDealerScore(true) < 17)
        //         {
        //             var dealerCard = _deck.DrawCard();
        //             gameSession.Dealer.Hand.Add(dealerCard);
        //         }
        //
        //         var dealerScore = gameSession.GetDealerScore(true);
        //         var result = dealerScore == 21
        //             ? "Dealer also reached 21! It's a draw."
        //             : "Player Wins!";
        //
        //         return Ok(new
        //         {
        //             Message = "Player reached 21. Dealer's turn completed.",
        //             SessionId = sessionId,
        //             BetAmount = request.BetAmount,
        //             PlayerHand = gameSession.GetPlayerHand(),
        //             DealerHand = gameSession.GetDealerHand(true),
        //             PlayerScore = gameSession.GetPlayerScore(),
        //             DealerScore = dealerScore,
        //             Result = result
        //         });
        //     }
        //
        //     // Normal oyun başlangıcı
        //     return Ok(new
        //     {
        //         Message = "Game started",
        //         SessionId = sessionId,
        //         BetAmount = request.BetAmount,
        //         PlayerHand = gameSession.GetPlayerHand(),
        //         DealerHand = gameSession.GetDealerHand(false),
        //         PlayerScore = gameSession.GetPlayerScore(),
        //         DealerScore = gameSession.GetDealerScore()
        //     });
        // }

        // [HttpPost("player-hit")]
        // public IActionResult PlayerHit([FromBody] RequestSession request)
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
        //     var result = gameSession.PlayerHit(_deck);
        //
        //     // Oyuncu 21'e ulaştıysa krupiye hamlelerini yap
        //     if (result == "Player Wins with 21!")
        //     {
        //         while (gameSession.GetDealerScore(true) < 17)
        //         {
        //             var dealerCard = _deck.DrawCard();
        //             gameSession.Dealer.Hand.Add(dealerCard);
        //         }
        //
        //         var dealerScore = gameSession.GetDealerScore(true);
        //         var finalResult = dealerScore == 21
        //             ? "Dealer also reached 21! It's a draw."
        //             : "Player Wins!";
        //
        //         return Ok(new
        //         {
        //             Message = "Player reached 21. Dealer's turn completed.",
        //             PlayerHand = gameSession.GetPlayerHand(),
        //             PlayerScore = gameSession.GetPlayerScore(),
        //             DealerHand = gameSession.GetDealerHand(true),
        //             DealerScore = dealerScore,
        //             Result = finalResult
        //         });
        //     }
        //
        //     // Oyuncu başarısız olduysa
        //     if (result == "Player Bust")
        //     {
        //         return Ok(new
        //         {
        //             Message = "Player bust. Dealer wins.",
        //             PlayerHand = gameSession.GetPlayerHand(),
        //             PlayerScore = gameSession.GetPlayerScore(),
        //             DealerHand = gameSession.GetDealerHand(true),
        //             DealerScore = gameSession.GetDealerScore(true),
        //             Result = "Dealer Wins!"
        //         });
        //     }
        //
        //     // Oyuncunun hamlesi devam ediyorsa
        //     return Ok(new
        //     {
        //         PlayerHand = gameSession.GetPlayerHand(),
        //         PlayerScore = gameSession.GetPlayerScore(),
        //         Result = result
        //     });
        // }

    [HttpPost("hit-dealer")]
    public IActionResult HitDealer([FromBody] RequestSession request)
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

        var newCard = _deck.DrawCard();
        gameSession.Dealer.Hand.Add(newCard);
        var dealerScore = gameSession.GetDealerScore(true);

        return Ok(new
        {
            NewCard = new { Rank = newCard.Rank, Suit = newCard.Suit, value = newCard.Value },
            DealerScore = dealerScore
        });
    }

    [HttpPost("start-game")]
    public IActionResult StartGame([FromBody] RequestSession request)
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

        gameSession.StartGame(_deck);

        return Ok(new { Message = "Game started successfully." });
    }

    [HttpPost("stay")]
    public IActionResult Stay([FromBody] RequestSession request)
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

        // Oyunun başlamış olup olmadığını kontrol et
        if (!gameSession.IsGameStarted)
        {
            return BadRequest(new { Message = "The game has not started. Please start the game first." });
        }

        // Oyuncu kalmayı seçtiği için krupiye hamlelerini yapar
        var result = gameSession.PlayerStay(_deck);

        return Ok(new
        {
            Message = "Player stays. Dealer's turn.",
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true), // Krupiyenin ikinci kartı açık
            DealerScore = gameSession.GetDealerScore(true),
            Result = result
        });
    }
}


// [HttpPost("player-hit")]
// public IActionResult PlayerHit([FromBody] RequestSession request)
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
//     // Oyunun başlamış olup olmadığını kontrol et
//     if (!gameSession.IsGameStarted)
//     {
//         return BadRequest(new { Message = "The game has not started. Please start the game first." });
//     }
//
//     var result = gameSession.PlayerHit(_deck);
//
//     return Ok(new
//     {
//         PlayerHand = gameSession.GetPlayerHand(),
//         PlayerScore = gameSession.GetPlayerScore(),
//         Result = result
//     });
// }

// [HttpPost("player-hit")]
// public IActionResult PlayerHit([FromBody] RequestSession request)
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
//     var result = gameSession.PlayerHit(_deck);
//
//     return Ok(new
//     {
//         PlayerHand = gameSession.GetPlayerHand(),
//         PlayerScore = gameSession.GetPlayerScore(),
//         Result = result
//     });
// }
// [HttpPost("start-game")]
// public IActionResult StartGame([FromBody] RequestSession request)
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
//     gameSession.StartGame(_deck);
//
//     return Ok(new { Message = "Game started successfully." });
// }


// [HttpPost("stay")]
// public async Task<IActionResult> Stay([FromBody] RequestSession request)
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
//     // Krupiyenin ikinci kartını aç
//     var result = gameSession.PlayerStay(_deck);
//
//     return Ok(new
//     {
//         Message = "Player stays",
//         Result = result,
//         PlayerHand = gameSession.GetPlayerHand(),
//         PlayerScore = gameSession.GetPlayerScore(),
//         DealerHand = gameSession.GetDealerHand(true), // Krupiyenin ikinci kartı açık
//         DealerScore = gameSession.GetDealerScore(true) // İkinci kart açıkken skoru al
//     });
// }

// [HttpPost("stay")]
// public async Task<IActionResult> Stay([FromBody] RequestSession request)
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
//     var result = gameSession.PlayerStay(_deck);
//
//     return Ok(new
//     {
//         Message = "Player stays",
//         Result = result,
//         PlayerHand = gameSession.GetPlayerHand(),
//         PlayerScore = gameSession.GetPlayerScore(),
//         DealerHand = gameSession.GetDealerHand(true), // Krupiyenin ikinci kartı açık
//         DealerScore = gameSession.GetDealerScore()
//     });
// }

// [HttpPost("new-game")]
// public IActionResult StartNewGame([FromBody] NewGameRequest request)
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
//     if (request.BetAmount <= 0)
//     {
//         return BadRequest(new { Message = "Bet amount must be greater than zero." });
//     }
//
//     var gameSession = new GameSession(new CasualMode());
//     gameSession.SetBetAmount(request.BetAmount);
//     _sessionManager.UpdateGameSession(sessionId, gameSession);
//
//     gameSession.StartNewSession();
//     gameSession.StartGame(_deck);
//
//     return Ok(new
//     {
//         Message = "Game started",
//         SessionId = sessionId,
//         BetAmount = request.BetAmount, // Bahis tutarını yanıt olarak gönder
//         PlayerHand = gameSession.GetPlayerHand(),
//         DealerHand = gameSession.GetDealerHand(false),
//         PlayerScore = gameSession.GetPlayerScore(),
//         DealerScore = gameSession.GetDealerScore()
//     });
// }
// [HttpPost("player-hit")]
// public IActionResult PlayerHit([FromBody] RequestSession request)
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
//     var result = gameSession.PlayerHit(_deck);
//
//     // Oyuncu 21'e ulaştıysa krupiye kartlarını çeker
//     if (result == "Player Wins with 21!")
//     {
//         while (gameSession.GetDealerScore() < 21)
//         {
//             _deck.DrawCard();
//             gameSession.Dealer.Hand.Add(_deck.DrawCard());
//         }
//
//         var dealerResult = gameSession.GetDealerScore() == 21
//             ? "Dealer also reached 21! It's a draw."
//             : "Player Wins!";
//
//         return Ok(new
//         {
//             PlayerHand = gameSession.GetPlayerHand(),
//             PlayerScore = gameSession.GetPlayerScore(),
//             DealerHand = gameSession.GetDealerHand(true),
//             DealerScore = gameSession.GetDealerScore(true),
//             Result = dealerResult
//         });
//     }
//
//     return Ok(new
//     {
//         PlayerHand = gameSession.GetPlayerHand(),
//         PlayerScore = gameSession.GetPlayerScore(),
//         Result = result
//     });
// }