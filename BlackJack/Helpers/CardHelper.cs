
namespace BlackJack.Helpers
{
    public static class CardHelper
    {
        public static int CalculateHandScore(List<Card> hand)
        {
            if (hand == null || hand.Count == 0) return 0; // Boş el kontrolü

            int score = 0;
            int aceCount = 0;

            foreach (var card in hand)
            {
                if (card == null) continue; // Null kartlar atlanır
                score += card.Value;
                if (card.Rank == "A") aceCount++; // As sayısını takip et
            }

            // Eğer skor 21'i aşarsa ve as varsa, as'ın değerini 11'den 1'e düşür
            while (score > 21 && aceCount > 0)
            {
                score -= 10;
                aceCount--;
            }

            return score;
        }
    }
}
