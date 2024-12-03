
namespace BlackJack.Service
{
    public interface IGameMode
    {
        void InitializeGame(Player player, Dealer dealer, Deck deck);
        string HandlePlayerMove(Player player, Dealer dealer, Deck deck, string move);
        string DetermineOutcome(Player player, Dealer dealer, Deck deck);
    }

}
