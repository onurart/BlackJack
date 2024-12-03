
using BlackJack.Models;

namespace BlackJack.Service
{
    public class CasualMode : IGameMode
    {
        public void InitializeGame(Player player, Dealer dealer, Deck deck)
        {
            deck.Shuffle();
            player.Hand = deck.DealInitialCards();
            dealer.Hand = deck.DealInitialCards();
        }

        public string HandlePlayerMove(Player player, Dealer dealer, Deck deck, string move)
        {
            if (move == "Hit")
            {
                player.Hand.Add(deck.DrawCard());
                if (player.Score > 21)
                    return "Player Bust";
            }
            else if (move == "Stay")
            {
                return DetermineOutcome(player, dealer, deck);
            }

            return "Continue";
        }

        public string DetermineOutcome(Player player, Dealer dealer, Deck deck)
        {
            while (dealer.Score < 17)
            {
                dealer.Hand.Add(deck.DrawCard());
            }

            if (dealer.Score > 21)
            {
                return "Player Wins! Dealer busts with " + dealer.Score;
            }
            else if (player.Score > dealer.Score)
            {
                return "Player Wins with " + player.Score + " vs Dealer's " + dealer.Score;
            }
            else if (player.Score == dealer.Score)
            {
                return "Draw! Both Player and Dealer have " + player.Score;
            }
            else
            {
                return "Dealer Wins with " + dealer.Score + " vs Player's " + player.Score;
            }
        }

    }
}     
    