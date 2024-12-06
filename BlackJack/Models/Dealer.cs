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