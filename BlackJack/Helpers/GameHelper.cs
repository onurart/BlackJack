namespace BlackJack.Helpers;

public class GameHelper
{
    private readonly Deck _deck;
    private readonly SessionManager _sessionManager;
    public GameHelper(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _deck = new Deck();
    }
    public IActionResult EndGameWithPlayerResult(GameSession gameSession, string result)
    {
        gameSession.EndGame();
        var isPlayerWin = result.Contains("Player Wins");
        var isDraw = result.Contains("Draw");
        var payout = gameSession.CalculatePayout(isPlayerWin, isDraw);
        return new OkObjectResult(new
        {
            Message = result,
            PlayerHand = gameSession.GetPlayerHand(),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerHand = gameSession.GetDealerHand(true),
            DealerScore = gameSession.GetDealerScore(true),
            BetAmount = payout,
            Result = result
        });
    }
    public class DealerResult
    {
        public int DealerScore { get; set; }
        public decimal BetAmount { get; set; }
        public string Result { get; set; }
    }

    public DealerResult EndGameWithDealerPlay(GameSession gameSession)
    {
        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        var dealerFinalScore = gameSession.GetDealerScore(true);
        var isPlayerWin = dealerFinalScore > 21 || gameSession.GetPlayerScore() > dealerFinalScore;
        var isDraw = gameSession.GetPlayerScore() == dealerFinalScore;

        var payout = gameSession.CalculatePayout(isPlayerWin, isDraw);

        gameSession.EndGame();

        return new DealerResult
        {
            DealerScore = dealerFinalScore,
            BetAmount = payout,
            Result = isPlayerWin ? "Player Wins!" : (isDraw ? "Draw" : "Dealer Wins!")
        };
    }
    public IActionResult EndGameWithBlackjack(GameSession gameSession, Guid sessionId)
    {
        while (gameSession.GetDealerScore(true) < 17)
        {
            var dealerCard = _deck.DrawCard();
            gameSession.Dealer.Hand.Add(dealerCard);
        }

        var dealerScore = gameSession.GetDealerScore(true);
        var result = dealerScore == 21
            ? "Dealer also reached 21! It's a draw."
            : "Player Wins with Blackjack!";

        gameSession.EndGame();

        return new OkObjectResult(new
        {
            Message = "Player reached 21. Game over.",
            SessionId = sessionId,
            BetAmount = gameSession.GetBetAmount() * 2,
            PlayerHand = gameSession.GetPlayerHand(),
            DealerHand = gameSession.GetDealerHand(true),
            PlayerScore = gameSession.GetPlayerScore(),
            DealerScore = dealerScore,
            Result = result,
            IsGameOver = true
        });
    }
    
    public IActionResult? ValidateSessionAndBet(string sessionId, decimal betAmount)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return new BadRequestObjectResult(new { Message = "Session ID is required." });
        }

        if (!Guid.TryParse(sessionId, out _))
        {
            return new BadRequestObjectResult(new { Message = "Invalid Session ID format." });
        }

        if (!_sessionManager.ValidateSession(Guid.Parse(sessionId)))
        {
            return new BadRequestObjectResult(new { Message = "Invalid session ID" });
        }

        if (betAmount <= 0 || betAmount > 10000000) // Örnek bir üst sınır
        {
            return new BadRequestObjectResult(new { Message = "Bet amount must be between 1 and 10,000." });
        }

        return null;
    }

    public GameSession? GetValidatedGameSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || !Guid.TryParse(sessionId, out var guid) || !_sessionManager.ValidateSession(guid))
        {
            return null;
        }

        return _sessionManager.GetGameSession(guid);
    }
}