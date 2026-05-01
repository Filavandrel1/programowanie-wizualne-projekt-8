using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public partial class BlackjackViewModel : GameViewModelBase
{
    public override string Title => "Oczko (Blackjack)";
    public override string Description =>
        "Dobierz karty jak najblizej 21, ale nie przekrocz. Komputer gra na tych samych zasadach.";
    public override string GameName => "Oczko";

    private const int Target = 21;

    private static readonly string[] Ranks =
        { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
    private static readonly string[] Suits = { "\u2660", "\u2665", "\u2666", "\u2663" };

    private readonly Random _rng = new();
    private readonly List<string> _deck = new();

    public ObservableCollection<string> PlayerHand { get; } = new();
    public ObservableCollection<string> DealerHand { get; } = new();

    /// <summary>
    /// To, co aktualnie widzi gracz w rece krupiera: druga karta jest zastapiona
    /// rewersem (znak "🂠"), dopoki gracz nie zakonczy swojej tury.
    /// </summary>
    public ObservableCollection<string> DealerVisibleHand { get; } = new();

    private const string CardBack = "\U0001F0A0";

    [ObservableProperty] private int _playerScore;
    [ObservableProperty] private int _dealerScore;
    /// <summary>Suma oczek z odkrytych kart krupiera — pokazujemy ja w UI.</summary>
    [ObservableProperty] private int _dealerVisibleScore;
    [ObservableProperty] private bool _isHoleRevealed;
    [ObservableProperty] private bool _isRoundActive;
    [ObservableProperty] private bool _isRoundOver;

    public BlackjackViewModel()
    {
        Status = "Nacisnij \"Rozdaj\", aby rozpoczac runde.";
    }

    protected override void DealCore()
    {
        BuildAndShuffleDeck();
        PlayerHand.Clear();
        DealerHand.Clear();

        PlayerHand.Add(DrawCard());
        DealerHand.Add(DrawCard());
        PlayerHand.Add(DrawCard());
        DealerHand.Add(DrawCard());

        IsHoleRevealed = false;
        RebuildDealerVisible();
        RecalculateScores();
        IsRoundOver = false;
        IsRoundActive = true;

        if (PlayerScore == Target)
        {
            FinishRoundWithDealerTurn();
        }
        else
        {
            Status = $"Twoje oczka: {PlayerScore}. Dobierasz czy stop?";
        }

        NotifyCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanHit))]
    private void Hit()
    {
        if (!CanHit()) return;
        PlayerHand.Add(DrawCard());
        RecalculateScores();

        if (PlayerScore > Target)
        {
            IsRoundActive = false;
            IsRoundOver = true;
            Status = $"Fura! Przekroczyles 21 ({PlayerScore}). Przegrywasz.";
            RecordResult($"przegrana ({PlayerScore})");
            NotifyCommandsChanged();
            return;
        }

        if (PlayerScore == Target)
        {
            FinishRoundWithDealerTurn();
            return;
        }

        Status = $"Dobierasz: masz {PlayerScore}. Dobierasz dalej czy stop?";
        NotifyCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStand))]
    private void Stand()
    {
        if (!CanStand()) return;
        FinishRoundWithDealerTurn();
    }

    private bool CanHit() => IsRoundActive && !IsRoundOver;
    private bool CanStand() => IsRoundActive && !IsRoundOver;

    private void FinishRoundWithDealerTurn()
    {
        IsRoundActive = false;

        // Tura krupiera zaczyna sie od odkrycia drugiej karty.
        IsHoleRevealed = true;
        RebuildDealerVisible();

        while (true)
        {
            RecalculateScores();
            if (DealerScore >= Target) break;

            // Krupier dobiera tylko wtedy, gdy nie ma jeszcze wygranej pozycji.
            // - jesli przegrywa lub remisuje z graczem -> musi dobrac (mustChase),
            // - jesli juz prowadzi -> staje, nawet ponizej progu 17
            //   (nie ma sensu ryzykowac fury, gdy juz wygrywa).
            bool mustChase = DealerScore <= PlayerScore;
            if (!mustChase) break;

            DealerHand.Add(DrawCard());
            RebuildDealerVisible();
        }

        RecalculateScores();
        IsRoundOver = true;

        string verdict;
        string history;
        if (DealerScore > Target)
        {
            verdict = $"Krupier ma fure ({DealerScore}). Wygrywasz!";
            history = $"wygrana ({PlayerScore} vs fura)";
        }
        else if (PlayerScore > DealerScore)
        {
            verdict = $"Wygrywasz! {PlayerScore} vs {DealerScore}.";
            history = $"wygrana ({PlayerScore} vs {DealerScore})";
        }
        else if (PlayerScore < DealerScore)
        {
            verdict = $"Krupier wygrywa: {DealerScore} vs {PlayerScore}.";
            history = $"przegrana ({PlayerScore} vs {DealerScore})";
        }
        else
        {
            verdict = $"Remis na {PlayerScore}.";
            history = $"remis ({PlayerScore})";
        }

        Status = verdict + " Nacisnij \"Rozdaj\", aby zagrac ponownie.";
        RecordResult(history);
        NotifyCommandsChanged();
    }

    private void NotifyCommandsChanged()
    {
        HitCommand.NotifyCanExecuteChanged();
        StandCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsRoundActiveChanged(bool value) => NotifyCommandsChanged();
    partial void OnIsRoundOverChanged(bool value) => NotifyCommandsChanged();

    private void BuildAndShuffleDeck()
    {
        _deck.Clear();
        foreach (var rank in Ranks)
            foreach (var suit in Suits)
                _deck.Add(rank + suit);

        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    private string DrawCard()
    {
        if (_deck.Count == 0)
            BuildAndShuffleDeck();
        var card = _deck[^1];
        _deck.RemoveAt(_deck.Count - 1);
        return card;
    }

    private void RecalculateScores()
    {
        PlayerScore = ScoreHand(PlayerHand);
        DealerScore = ScoreHand(DealerHand);
        // Widoczny wynik krupiera: jesli druga karta zakryta, liczymy tylko z odkrytych.
        if (IsHoleRevealed)
        {
            DealerVisibleScore = DealerScore;
        }
        else
        {
            // Wszystko poza indeksem 1 jest odkryte.
            var visible = new List<string>();
            for (int i = 0; i < DealerHand.Count; i++)
            {
                if (i == 1) continue;
                visible.Add(DealerHand[i]);
            }
            DealerVisibleScore = ScoreHand(visible);
        }
    }

    private void RebuildDealerVisible()
    {
        DealerVisibleHand.Clear();
        for (int i = 0; i < DealerHand.Count; i++)
        {
            bool isHole = (i == 1) && !IsHoleRevealed;
            DealerVisibleHand.Add(isHole ? CardBack : DealerHand[i]);
        }
    }

    partial void OnIsHoleRevealedChanged(bool value)
    {
        RebuildDealerVisible();
        RecalculateScores();
    }

    private static int ScoreHand(IEnumerable<string> hand)
    {
        int total = 0;

        foreach (var card in hand)
        {
            string rank = card[..^1];
            // Niestandardowa wycena figur: K=4, Q=3, J=2, As=11.
            switch (rank)
            {
                case "A":
                    total += 11;
                    break;
                case "K":
                    total += 4;
                    break;
                case "Q":
                    total += 3;
                    break;
                case "J":
                    total += 2;
                    break;
                default:
                    total += int.Parse(rank);
                    break;
            }
        }

        return total;
    }
}
