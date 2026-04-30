using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace projekt8plsdzialaj.Models;

public enum Suit
{
    Spades,
    Hearts,
    Diamonds,
    Clubs
}

public partial class SolitaireCard : ObservableObject
{
    public Suit Suit { get; init; }

    /// <summary>1 = As, 2..10, 11 = J, 12 = Q, 13 = K.</summary>
    public int Value { get; init; }

    public string Rank => Value switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => Value.ToString()
    };

    public string SuitSymbol => Suit switch
    {
        Suit.Spades => "♠",
        Suit.Hearts => "♥",
        Suit.Diamonds => "♦",
        Suit.Clubs => "♣",
        _ => "?"
    };

    public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;
    public bool IsBlack => !IsRed;

    [ObservableProperty] private bool _isFaceUp;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Thickness _stackMargin;
}
