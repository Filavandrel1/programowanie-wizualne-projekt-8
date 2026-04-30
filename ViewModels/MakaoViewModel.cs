using System;

namespace projekt8plsdzialaj.ViewModels;

public partial class MakaoViewModel : GameViewModelBase
{
    public override string Title => "Makao";
    public override string Description => "Pozbądź się wszystkich kart, dopasowując kolor lub figurę.";

    private static readonly string[] Ranks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
    private readonly Random _rng = new();

    protected override void DealCore()
    {
        var hand = new string[5];
        for (int i = 0; i < hand.Length; i++)
            hand[i] = Ranks[_rng.Next(Ranks.Length)] + Suits[_rng.Next(Suits.Length)];

        var top = Ranks[_rng.Next(Ranks.Length)] + Suits[_rng.Next(Suits.Length)];
        Status = $"Twoja ręka: {string.Join("  ", hand)}\nKarta na stole: {top}";
    }
}
