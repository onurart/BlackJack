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
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _random.Next(0, i + 1);
            var temp = _cards[i];
            _cards[i] = _cards[j];
            _cards[j] = temp;
        }
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