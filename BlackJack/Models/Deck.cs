using BlackJack.Models;

namespace BlackJack.Models;

public class Deck
{
    private List<Card> _cards;
    public bool IsEmpty => !_cards.Any();

    private Random _random;
    public Deck()
    {
        _cards = new List<Card>();
        _random = new Random();
        InitializeDeck();
        Shuffle();
    }
    private void InitializeDeck()
    {
        string[] suits = { "hearts", "diamonds", "clubs", "spades" };
        string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        int[] values = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10, 10, 11 };
        foreach (var suit in suits)
        {
            for (int i = 0; i < ranks.Length; i++)
            {
                _cards.Add(new Card(suit, ranks[i], values[i]));
            }
        }
    }
    public void Shuffle()
    {
        _cards = _cards.OrderBy(x => _random.Next()).ToList();
    }
    public Card DrawCard()
    {
        if (_cards.Count == 0) InitializeDeck();
        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }
    public List<Card> DealInitialCards()
    {
        return new List<Card> { DrawCard(), DrawCard() };
    }
}