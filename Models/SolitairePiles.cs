using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace projekt8plsdzialaj.Models;

public partial class TableauPile : ObservableObject
{
    public int Index { get; init; }
    public ObservableCollection<SolitaireCard> Cards { get; } = new();
}

public partial class FoundationPile : ObservableObject
{
    public Suit Suit { get; init; }
    public ObservableCollection<SolitaireCard> Cards { get; } = new();

    public string SuitSymbol => Suit switch
    {
        Suit.Spades => "♠",
        Suit.Hearts => "♥",
        Suit.Diamonds => "♦",
        Suit.Clubs => "♣",
        _ => "?"
    };

    public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;

    [ObservableProperty] private SolitaireCard? _topCard;
    [ObservableProperty] private int _count;

    public void RefreshDerived()
    {
        TopCard = Cards.Count > 0 ? Cards[^1] : null;
        Count = Cards.Count;
    }
}
