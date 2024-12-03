// using BlackJack.Models;
//
// namespace BlackJack.Service
// {
//     public class SimpleGameMode : IGameMode
//
//     {
//         public string DetermineOutcome(Player player, Dealer dealer, Deck deck)
//         {
//             while (dealer.Score < 17)
//             {
//                 dealer.Hand.Add(deck.DrawCard());
//             }
//
//             if (dealer.Score > 21 || player.Score > dealer.Score)
//             {
//                 return "Player Wins!";
//             }
//             else if (player.Score < dealer.Score)
//             {
//                 return "Dealer Wins!";
//             }
//             else
//             {
//                 return "It's a Tie!";
//             }
//         }
//         public string HandlePlayerMove(Player player, Dealer dealer, Deck deck, string move)
//         {
//             throw new NotImplementedException();
//         }
//
//         public void InitializeGame(Player player, Dealer dealer, Deck deck)
//         {
//             player.Hand.AddRange(deck.DealInitialCards());
//             dealer.Hand.AddRange(deck.DealInitialCards());
//         }
//     }
// }
//
//
//
