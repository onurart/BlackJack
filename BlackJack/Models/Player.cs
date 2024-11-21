namespace BlackJack.Models
{
    public class Player
    {
        public List<Card> Hand { get; set; }
        public int Score => CalculateScore();

        public Player()
        {
            Hand = new List<Card>();
        }
        private int CalculateScore()
        {
            int score = 0;
            int aceCount = 0;

            foreach (var card in Hand)
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