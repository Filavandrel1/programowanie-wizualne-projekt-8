using System;
using System.Collections.Generic;
using System.Linq;

namespace projekt8plsdzialaj.ViewModels;

public partial class BlackjackViewModel : GameViewModelBase
{
    public override string Title => "Oczko (Blackjack)";
    public override string Description => "Zbierz jak najwięcej oczek, ale nie przekrocz 21.";
    public override string GameName => "Oczko";

    private readonly Random _rng = new();

    protected override void DealCore()
    {
        var hand = new List<int>();
        int sum = 0;
        while (sum < 17)
        {
            int card = _rng.Next(2, 12);
            hand.Add(card);
            sum += card;
        }

        string verdict;
        string historyResult;
        if (sum == 21) { verdict = "Blackjack!"; historyResult = "wygrana"; }
        else if (sum > 21) { verdict = "Przegrana – fura!"; historyResult = "przegrana"; }
        else { verdict = "Stoisz na " + sum + "."; historyResult = "wygrana (" + sum + ")"; }

        Status = $"Karty: {string.Join(" + ", hand)} = {sum}\n{verdict}";
        RecordResult(historyResult);
    }
}
