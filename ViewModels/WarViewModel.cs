using System;

namespace projekt8plsdzialaj.ViewModels;

public partial class WarViewModel : GameViewModelBase
{
    public override string Title => "Wojna";
    public override string Description => "Klasyczna gra karciana – kto ma wyższą kartę, ten wygrywa rundę.";

    private static readonly string[] Ranks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
    private readonly Random _rng = new();

    protected override void DealCore()
    {
        var p1 = Draw(out var p1Val);
        var p2 = Draw(out var p2Val);
        var result = p1Val > p2Val ? "Wygrywa Gracz 1!"
                   : p2Val > p1Val ? "Wygrywa Gracz 2!"
                   : "Remis – wojna!";
        Status = $"Gracz 1: {p1}   |   Gracz 2: {p2}\n{result}";
    }

    private string Draw(out int value)
    {
        value = _rng.Next(Ranks.Length);
        return Ranks[value] + Suits[_rng.Next(Suits.Length)];
    }
}
