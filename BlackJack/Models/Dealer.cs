using BlackJack.Helpers;

namespace BlackJack.Models
{
    public class Dealer
    {
        public List<Card> Hand { get; set; }
        public int Score => CardHelper.CalculateHandScore(Hand); // Skor hesaplama
        private bool _isSecondCardHidden;

        public Dealer()
        {
            Hand = new List<Card>();
        }

        public void HideSecondCard()
        {
            _isSecondCardHidden = true;
        }

        public void RevealSecondCard()
        {
            _isSecondCardHidden = false;
        }

        public List<Card> GetHandWithHiddenCard()
        {
            var handCopy = new List<Card>(Hand);
            if (_isSecondCardHidden && handCopy.Count > 1)
            {
                handCopy[1] = new Card("hidden", "?", 0); // İkinci kartı gizle
            }
            return handCopy;
        }
    }
}


// namespace BlackJack.Models
// {
//     public class Dealer
//     {
//         public List<Card> Hand { get; set; }
//         public int Score => CalculateScore();
//         private bool _isSecondCardHidden;
//         public Dealer()
//         {
//             Hand = new List<Card>();
//         }
//         public void HideSecondCard()
//         {
//             _isSecondCardHidden = true;
//         }
//         public void RevealSecondCard()
//         {
//             _isSecondCardHidden = false;
//         }
//         public List<Card> GetHandWithHiddenCard()
//         {
//             var handCopy = new List<Card>(Hand);
//             if (_isSecondCardHidden && handCopy.Count > 1)
//             {
//                 handCopy[1] = new Card("hidden", "?", 0); // Daha güvenli bir temsil
//             }
//             return handCopy;
//         }
//         public void AddCardToHand(Card card)
//         {
//             if (card == null) throw new ArgumentNullException(nameof(card));
//             Hand.Add(card);
//         }
//
//
//         private int CalculateScore()
//         {
//             int score = 0;
//             int aceCount = 0;
//             foreach (var card in Hand)
//             {
//                 if (card != null) 
//                 {
//                     score += card.Value;
//                     if (card.Rank == "A") aceCount++;
//                 }
//             }
//             while (score > 21 && aceCount > 0)
//             {
//                 score -= 10; 
//                 aceCount--;
//             }
//             return score;
//         }
//     }
// }