using System;

namespace projekt8plsdzialaj.ViewModels;

public partial class WarViewModel : GameViewModelBase
{
    public override string Title => "Wojna";
    public override string Description => "Klasyczna gra karciana – kto ma wyższą kartę, ten wygrywa rundę.";
    public override string GameName => "Wojna";

    private static readonly string[] Ranks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
    private readonly Random _rng = new();

    protected override void DealCore()
    {
        var p1 = Draw(out var p1Val);
        var p2 = Draw(out var p2Val);

        string outcome;
        string historyResult;
        if (p1Val > p2Val) { outcome = "Wygrywa Gracz 1!"; historyResult = "wygrana"; }
        else if (p2Val > p1Val) { outcome = "Wygrywa Gracz 2!"; historyResult = "przegrana"; }
        else { outcome = "Remis – wojna!"; historyResult = "remis"; }

        Status = $"Gracz 1: {p1}   |   Gracz 2: {p2}\n{outcome}";
        RecordResult(historyResult);
    }

    private string Draw(out int value)
    {
        value = _rng.Next(Ranks.Length);
        return Ranks[value] + Suits[_rng.Next(Suits.Length)];
    }
}
