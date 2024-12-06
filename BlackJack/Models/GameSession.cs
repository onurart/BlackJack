using BlackJack.Helpers;
namespace BlackJack.Models;
public class GameSession
{
    private List<List<Card>> _playerHands;
    private int _currentHandIndex;
    private decimal _betAmount;
    private decimal _originalBetAmount;
    private bool _isGameStarted;
    private bool _isGameOver;
    private bool _dealerSecondCardRevealed;
    private readonly decimal _houseEdge = 0.02m;
    private bool _hasDoubledDown = false;
    public bool HasDoubledDown => _hasDoubledDown;
    public bool IsGameOver => _isGameOver;
    public bool IsGameStarted => _isGameStarted;
    public List<List<Card>> PlayerHand => _playerHands;
    public int CurrentHandIndex => _currentHandIndex;
    public Player Player { get; private set; }
    public Dealer Dealer { get; private set; }
    private readonly IGameMode _gameMode;
    public GameSession(IGameMode gameMode)
    {
        _gameMode = gameMode ?? new CasualMode();
        _playerHands = new List<List<Card>>();
        _currentHandIndex = 0;
        _isGameStarted = false;
        _isGameOver = false;
        Player = new Player();
        Dealer = new Dealer();
        StartNewSession();
    }
    public void DoubleDown()
    {
        if (_hasDoubledDown)
        {
            throw new InvalidOperationException("You can only double down once.");
        }

        _betAmount *= 2;
        _hasDoubledDown = true;
    }
    public void StartNewSession()
    {
        _playerHands = new List<List<Card>> { new List<Card>() };
        _currentHandIndex = 0;
        _isGameStarted = false;
        _isGameOver = false;
        _dealerSecondCardRevealed = false;
        Player.Hand.Clear();
        Dealer.Hand.Clear();
    }
    public void SetBetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Bet amount must be greater than zero.");
        }
        _betAmount = amount;
        _originalBetAmount = amount;
    }
    public decimal GetBetAmount() => _betAmount;
    public decimal GetOriginalBetAmount() => _originalBetAmount;
    public void StartGame(Deck deck)
    {
        if (deck == null || deck.IsEmpty)
        {
            throw new InvalidOperationException("Deck is empty or not initialized.");
        }

        _gameMode.InitializeGame(Player, Dealer, deck);
        _playerHands[0] = new List<Card>(Player.Hand);
        _isGameStarted = true;
        _isGameOver = false;
    }
    public List<Card> GetPlayerHand()
    {
        if (_currentHandIndex >= 0 && _currentHandIndex < _playerHands.Count)
        {
            return _playerHands[_currentHandIndex];
        }

        if (_playerHands.Any())
        {
            return _playerHands.First(); 
        }

        return new List<Card>
        {
            new Card("unknown", "?", 0) 
        };
    }
    public int GetPlayerScore()
    {
        if (_currentHandIndex >= 0 && _currentHandIndex < _playerHands.Count)
        {
            return CardHelper.CalculateHandScore(_playerHands[_currentHandIndex]);
        } 
        return 0;
    }
    public string HitCurrentHand(Deck deck)
    {
        if (_currentHandIndex >= _playerHands.Count)
        {
            throw new InvalidOperationException("No more hands to play.");
        }

        var currentHand = _playerHands[_currentHandIndex];
        var newCard = deck.DrawCard();
        currentHand.Add(newCard);

        int handScore = CardHelper.CalculateHandScore(currentHand);

        if (handScore > 21) 
        {
            _currentHandIndex++; 
            if (_currentHandIndex < _playerHands.Count)
            {
                return $"Player Bust on Hand {_currentHandIndex}. Moving to Hand {_currentHandIndex + 1}.";
            }

            _isGameOver = true; 
            return "Player Bust. Game Over.";
        }

        if (handScore == 21) 
        {
            _currentHandIndex++;
            if (_currentHandIndex < _playerHands.Count)
            {
                return $"Player Wins with 21 on Hand {_currentHandIndex}. Moving to Hand {_currentHandIndex + 1}.";
            }

            _isGameOver = true; 
            return "Player Wins with 21. Game Over.";
        }

        return "Continue";
    }
    public string StayCurrentHand(Deck deck)
    {
        if (_currentHandIndex < _playerHands.Count - 1)
        {
            _currentHandIndex++; 
            return $"Stayed on Hand {_currentHandIndex}. Moving to Hand {_currentHandIndex + 1}.";
        }
        _dealerSecondCardRevealed = true;
        while (Dealer.Score < 17)
        {
            Dealer.Hand.Add(deck.DrawCard());
        }

        _isGameOver = true; 
        return EvaluateFinalOutcome();
    }
    public void Split(Deck deck)
    {
        if (!CanSplit())
        {
            throw new InvalidOperationException("Split is not available.");
        }

        var firstCard = _playerHands[_currentHandIndex][0];
        var secondCard = _playerHands[_currentHandIndex][1];

        var firstHand = new List<Card> { firstCard, deck.DrawCard() };
        var secondHand = new List<Card> { secondCard, deck.DrawCard() };

        _playerHands = new List<List<Card>> { firstHand, secondHand };
        _currentHandIndex = 0; // İlk ele odaklan
        _isGameStarted = true;
    }
    public bool CanSplit()
    {
        if (_playerHands.Count != 1 || _playerHands[0].Count != 2)
        {
            return false;
        }

        return _playerHands[0][0].Value == _playerHands[0][1].Value;
    }
    public List<Card> GetDealerHand(bool revealSecondCard = false)
    {
        if (revealSecondCard || _dealerSecondCardRevealed)
        {
            return Dealer.Hand;
        }

        return new List<Card>
        {
            Dealer.Hand[0],
            new Card("hidden", "?", 0)
        };
    }
    public int GetDealerScore(bool revealSecondCard = false)
    {
        int score = 0;
        int aceCount = 0;

        for (int i = 0; i < Dealer.Hand.Count; i++)
        {
            if (i == 1 && !revealSecondCard)
            {
                continue;
            }

            var card = Dealer.Hand[i];
            score += card.Value;

            if (card.Rank == "A")
            {
                aceCount++;
            }
        }

        while (score > 21 && aceCount > 0)
        {
            score -= 10;
            aceCount--;
        }

        return score;
    }
    private int CalculateHandScore(List<Card> hand)
    {
        if (hand == null || hand.Count == 0) return 0;

        int score = 0;
        int aceCount = 0;

        foreach (var card in hand)
        {
            if (card == null) continue; 
            score += card.Value;
            if (card.Rank == "A") aceCount++;
        }
        while (score > 21 && aceCount > 0)
        {
            score -= 10;
            aceCount--;
        }

        return score;
    }
    private string EvaluateFinalOutcome()
    {
        var results = new List<string>();
        foreach (var hand in _playerHands)
        {
            int handScore = CardHelper.CalculateHandScore(hand); 

            if (handScore > 21)
            {
                results.Add("Hand Bust");
            }
            else if (Dealer.Score > 21 || handScore > Dealer.Score)
            {
                results.Add("Player Won");
            }
            else if (handScore == Dealer.Score)
            {
                results.Add("Hand Draw");
            }
            else
            {
                results.Add("Dealer Wins");
            }
        }

        return string.Join(", ", results);
    }
    public void NextHand()
    {
        if (_currentHandIndex < _playerHands.Count - 1)
        {
            _currentHandIndex++;
        }
        else
        {
            _isGameOver = true;
        }
    }
    public void EndGame()
    {
        _isGameStarted = false;
        _isGameOver = true;
    }
    public decimal CalculatePayout(bool isPlayerWin, bool isDraw)
    {
        if (isDraw)
        {
            return _originalBetAmount;
        }
        if (isPlayerWin)
        {
            return _originalBetAmount * 2;
        }
        return 0; 
    } 
    public string PlayerHit(Deck deck)
     {
         if (!_isGameStarted)
         {
             throw new InvalidOperationException("The game has not started.");
         }

         var currentHand = _playerHands[_currentHandIndex];
         var newCard = deck.DrawCard();
         currentHand.Add(newCard);

         int handScore = CalculateHandScore(currentHand);

         if (handScore > 21)
         {
             NextHand();
             return "Player Bust";
         }

         if (handScore == 21)
         {
             NextHand();
             return "Player Wins with 21!";
         }

         return "Continue";
     }
}
