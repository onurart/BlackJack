using BlackJack.Exceptions;
using BlackJack.Service;

namespace BlackJack.Models
{
    public class GameSession
    {
        private bool _isGameStarted;
        private decimal _betAmount;
        private bool _dealerSecondCardRevealed; 
        private bool _isGameOver;
        public bool IsGameOver => _isGameOver;

        public bool IsGameStarted => _isGameStarted;
        public Guid SessionId { get; private set; }
        public Player Player { get; private set; }
        public Dealer Dealer { get; private set; }
        private readonly IGameMode _gameMode;
        private bool _hasDoubledDown = false;
        public bool HasDoubledDown => _hasDoubledDown;
        public GameSession(IGameMode gameMode)
        {
            _gameMode = gameMode ?? new CasualMode();
            _isGameStarted = false;
            _isGameOver = false;
            Player = new Player();
            Dealer = new Dealer();
            StartNewSession();
        }

        public void StartNewSession()
        {
            SessionId = Guid.NewGuid();
            Player.Hand.Clear();
            Dealer.Hand.Clear();
            _betAmount = 0;
            _dealerSecondCardRevealed = false;
            _isGameStarted = false; 
        }

        public bool ValidateBet(decimal betAmount)
        {
            if (betAmount <= 0)
            {
                throw new InvalidBetAmountException("Bet amount must be greater than zero.");
            }
            _betAmount = betAmount;
            return true;
        }

        public void SetBetAmount(decimal betAmount)
        {
            if (betAmount <= 0)
            {
                throw new InvalidBetAmountException("Bet amount must be greater than zero.");
            }
            _betAmount = betAmount;
        }

        public decimal GetBetAmount() => _betAmount;

        public void StartGame(Deck deck)
        {
            if (deck == null || deck.IsEmpty)
            {
                throw new InvalidOperationException("Deck is empty or not initialized.");
            }

            _gameMode.InitializeGame(Player, Dealer, deck);
            _isGameStarted = true; 
            _isGameOver = false; 

            
        }

        public List<Card> GetDealerHand(bool revealSecondCard = false)
        {
            if (revealSecondCard || _dealerSecondCardRevealed)
            {
                return Dealer.Hand;
            }

            return new List<Card> { Dealer.Hand[0], new Card("hidden", "?", 0) };
        }
        public string PlayerHit(Deck deck)
        {
            if (!_isGameStarted)
            {
                throw new InvalidOperationException("The game has not started.");
            }

            var newCard = deck.DrawCard();
            Player.Hand.Add(newCard);

            if (Player.Score > 21)
            {
                _isGameStarted = false;
                return "Player Bust";
            }

            if (Player.Score == 21)
            {
                while (Dealer.Score < 17)
                {
                    Dealer.Hand.Add(deck.DrawCard());
                }

                _isGameStarted = false; // Oyun sona erdi
                return Dealer.Score == 21 ? "Draw" : "Player Wins with 21!";
            }

            return "Continue";
        }

     
        
        
        public string PlayerStay(Deck deck)
        {
            if (!_isGameStarted)
            {
                throw new InvalidOperationException("The game has not started.");
            }

            _dealerSecondCardRevealed = true;

            while (Dealer.Score < 17)
            {
                Dealer.Hand.Add(deck.DrawCard());
            }

            _isGameStarted = false; 
            return _gameMode.DetermineOutcome(Player, Dealer, deck);
        }
        public void EndGame()
        {
            _isGameStarted = false;
            _isGameOver = true;  
        }

    

        public string DetermineOutcome()
        {
            if (!_isGameStarted)
            {
                throw new InvalidOperationException("The game has not started.");
            }

            return _gameMode.DetermineOutcome(Player, Dealer, new Deck());
        }

        public List<Card> GetPlayerHand() => Player.Hand;

        public int GetPlayerScore() => Player.Score;

        public int GetDealerScore(bool revealSecondCard = false)
        {
            if (!_isGameStarted && !revealSecondCard)
            {
                throw new InvalidOperationException("The game has not started, and dealer's second card cannot be revealed.");
            }

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
        public void DoubleDown()
        {
            if (_hasDoubledDown)
            {
                throw new InvalidOperationException("You can only double down once.");
            }

            _betAmount *= 2;
            _hasDoubledDown = true;
        }
    }
}

    // public string PlayerStay(Deck deck)
        // {
        //     if (!_isGameStarted)
        //     {
        //         throw new InvalidOperationException("The game has not started.");
        //     }
        //
        //     // Krupiyenin ikinci kartı açılır
        //     _dealerSecondCardRevealed = true;
        //
        //     // Krupiye 17'ye ulaşana kadar kart çeker
        //     while (Dealer.Score < 17)
        //     {
        //         Dealer.Hand.Add(deck.DrawCard());
        //     }
        //
        //     // Sonuç belirlenir
        //     var result = _gameMode.DetermineOutcome(Player, Dealer, deck);
        //
        //     // Oyun sona erdiği için bayrak güncellenir
        //     _isGameStarted = false;
        //
        //     return result;
        // }
        // public string PlayerStay(Deck deck)
        // {
        //     if (!_isGameStarted|| _isGameOver)
        //     {
        //         throw new InvalidOperationException("The game has not started.");
        //     }
        //     _dealerSecondCardRevealed = true;
        //     while (Dealer.Score < 17)
        //     {
        //         Dealer.Hand.Add(deck.DrawCard());
        //     }
        //     var result = _gameMode.DetermineOutcome(Player, Dealer, deck);
        //     _isGameStarted = false;
        //     _isGameOver = true; // Oyuncu hamlesini bitirdi, oyun sona erdi
        //     return _gameMode.DetermineOutcome(Player, Dealer, deck);
        // }

        
        // public string PlayerStay(Deck deck)
        // {
        //     if (!_isGameStarted)
        //     {
        //         throw new InvalidOperationException("The game has not started.");
        //     }
        //
        //     _dealerSecondCardRevealed = true;
        //
        //     while (Dealer.Score < 17)
        //     {
        //         Dealer.Hand.Add(deck.DrawCard());
        //     }
        //
        //     _isGameStarted = false; // Oyun sona erdi
        //     return _gameMode.DetermineOutcome(Player, Dealer, deck);
        // }
   // public string PlayerHit(Deck deck)
        // {
        //     if (!_isGameStarted)
        //     {
        //         throw new InvalidOperationException("The game has not started.");
        //     }
        //
        //     var newCard = deck.DrawCard();
        //     Player.Hand.Add(newCard);
        //
        //     if (Player.Score > 21)
        //     {
        //         _isGameStarted = false;
        //         return "Player Bust";
        //     }
        //
        //     if (Player.Score == 21)
        //     {
        //         _isGameStarted = false;
        //         return "Player Wins with 21!";
        //     }
        //
        //     return "Continue";
        // }
        //
        //
        //
        // public string PlayerHit(Deck deck)
        // {
        //     if (!_isGameStarted)
        //     {
        //         throw new InvalidOperationException("The game has not started. Please start the game before making a move.");
        //     }
        //
        //     var newCard = deck.DrawCard();
        //     Player.Hand.Add(newCard);
        //
        //     if (Player.Score > 21)
        //     {
        //         _isGameStarted = false; // Oyun sona eriyor
        //         return "Player Bust";
        //     }
        //
        //     if (Player.Score == 21)
        //     {
        //         _isGameStarted = false; // Oyun sona eriyor
        //         return "Player Wins with 21!";
        //     }
        //
        //     return "Continue";
        // }
        
        // public string PlayerHit(Deck deck)
        // {
        //     if (!_isGameStarted|| _isGameOver)
        //     {
        //         throw new InvalidOperationException("The game has not started.");
        //     }
        //
        //     var newCard = deck.DrawCard();
        //     Player.Hand.Add(newCard);
        //
        //     if (Player.Score > 21)
        //     {
        //         _isGameOver = true;
        //         return "Player Bust";
        //     }
        //
        //     if (Player.Score == 21)
        //     {
        //         return "Player Wins with 21!"; 
        //     }
        //
        //     return "Continue";
        // }

// using BlackJack.Exceptions;
// using BlackJack.Service;
//
// namespace BlackJack.Models
// {
//     public class GameSession
//     {
//         private bool _isGameStarted;
//
//         public bool IsGameStarted => _isGameStarted;
//         
//         public Guid SessionId { get; private set; }
//         public Player Player { get; private set; }
//         public Dealer Dealer { get; private set; }
//         private decimal _betAmount;
//         private readonly IGameMode _gameMode;
//         private bool _dealerSecondCardRevealed; 
//
//         public GameSession(IGameMode gameMode)
//         {
//             _gameMode = gameMode ?? new CasualMode();
//             Player = new Player();
//             Dealer = new Dealer();
//             StartNewSession();
//         }
//
//         public void StartNewSession()
//         {
//             SessionId = Guid.NewGuid();
//             Player.Hand.Clear();
//             Dealer.Hand.Clear();
//             _betAmount = 0;
//             _dealerSecondCardRevealed = false; 
//         }
//
//         public bool ValidateBet(decimal betAmount)
//         {
//             if (betAmount <= 0)
//             {
//                 throw new InvalidBetAmountException("Bet amount must be greater than zero.");
//             }
//             _betAmount = betAmount;
//             return true;
//         }
//
//         public void SetBetAmount(decimal betAmount)
//         {
//             if (betAmount <= 0)
//             {
//                 throw new InvalidBetAmountException("Bet amount must be greater than zero.");
//             }
//             _betAmount = betAmount;
//         }
//
//         public decimal GetBetAmount()
//         {
//             return _betAmount;
//         }
//
//         public void StartGame(Deck deck)
//         {
//             _gameMode.InitializeGame(Player, Dealer, deck);
//         }
//
//         public List<Card> GetDealerHand(bool revealSecondCard = false)
//         {
//             if (revealSecondCard || _dealerSecondCardRevealed)
//             {
//                 return Dealer.Hand;
//             }
//             else
//             {
//                 return new List<Card> { Dealer.Hand[0], new Card("Hidden", "?", 0) };
//             }
//         }
//
//         public string PlayerHit(Deck deck)
//         {
//             var newCard = deck.DrawCard();
//             Player.Hand.Add(newCard);
//
//             if (Player.Score > 21)
//                 return "Player Bust";
//             if (Player.Score == 21)
//                 return "Player Wins with 21!";
//
//             return "Continue";
//         }
//
//         public string PlayerStay(Deck deck)
//         {
//             _dealerSecondCardRevealed = true; // İkinci kart açıldı
//
//             while (Dealer.Score < 17)
//             {
//                 Dealer.Hand.Add(deck.DrawCard());
//             }
//
//             return _gameMode.DetermineOutcome(Player, Dealer, deck);
//         }
//
//         public string DetermineOutcome()
//         {
//             return _gameMode.DetermineOutcome(Player, Dealer, new Deck());
//         }
//
//         public List<Card> GetPlayerHand() => Player.Hand;
//         public int GetPlayerScore() => Player.Score;
//         public int GetDealerScore(bool revealSecondCard = false)
//         {
//             int score = 0;
//             int aceCount = 0;
//
//             for (int i = 0; i < Dealer.Hand.Count; i++)
//             {
//                 if (i == 1 && !revealSecondCard)
//                 {
//                     continue;
//                 }
//
//                 var card = Dealer.Hand[i];
//                 score += card.Value;
//
//                 if (card.Rank == "A")
//                 {
//                     aceCount++;
//                 }
//             }
//
//             while (score > 21 && aceCount > 0)
//             {
//                 score -= 10;
//                 aceCount--;
//             }
//
//             return score;
//         }
//     }
// }
