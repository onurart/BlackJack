
namespace BlackJack.Helpers
{
    public static class CardHelper
    {
        public static int CalculateHandScore(List<Card> hand)
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
    }
}
