using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

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
    // --- Confetti emitter state ---
    public ObservableCollection<Confetto> Confetti { get; } = new();
    private DispatcherTimer? _confettiTimer;

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
    // Flaga, gdy gracz wygrał rundę — do wyświetlania overlayu zwycięstwa.
    [ObservableProperty] private bool _isPlayerWinner;
    [ObservableProperty] private bool _isRoundActive;
    [ObservableProperty] private bool _isRoundOver;

    public BlackjackViewModel()
    {
        Status = "Nacisnij \"Rozdaj\", aby rozpoczac runde.";
    }

    [RelayCommand(CanExecute = nameof(CanPeek))]
    private void Peek()
    {
        if (!CanPeek()) return;

        // 50% chance to be caught: if caught, dealer immediately reveals hole and wins the round.
        bool caught = _rng.NextDouble() < 0.5;
        IsHoleRevealed = true;
        RebuildDealerVisible();
        RecalculateScores();

        if (caught)
        {
            // Caught peeking -> dealer wins immediately
            IsRoundActive = false;
            IsRoundOver = true;
            IsPlayerWinner = false;
            Status = "Zostales zlapany na podgladzie! Krupier wygrywa.";
            RecordResult($"przegrana (zlapany przy podgladzie {PlayerScore} vs {DealerScore})");
            NotifyCommandsChanged();
            return;
        }

        // Not caught: player sees hole but game continues (player may choose to hit/stand).
        Status = $"Udalo sie! Zobaczyles karte krupiera: {DealerHand[1]}. Twoje oczka: {PlayerScore}.";
        NotifyCommandsChanged();
    }

    private bool CanPeek() => IsRoundActive && !IsRoundOver && !IsHoleRevealed;

    partial void OnIsPlayerWinnerChanged(bool value)
    {
        if (value)
            StartConfetti();
        else
            StopConfetti();
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
    IsPlayerWinner = false;
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
            IsPlayerWinner = false;
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
            IsPlayerWinner = true;
        }
        else if (PlayerScore > DealerScore)
        {
            verdict = $"Wygrywasz! {PlayerScore} vs {DealerScore}.";
            history = $"wygrana ({PlayerScore} vs {DealerScore})";
            IsPlayerWinner = true;
        }
        else if (PlayerScore < DealerScore)
        {
            verdict = $"Krupier wygrywa: {DealerScore} vs {PlayerScore}.";
            history = $"przegrana ({PlayerScore} vs {DealerScore})";
            IsPlayerWinner = false;
        }
        else
        {
            verdict = $"Remis na {PlayerScore}.";
            history = $"remis ({PlayerScore})";
            IsPlayerWinner = false;
        }

        Status = verdict + " Nacisnij \"Rozdaj\", aby zagrac ponownie.";
        RecordResult(history);
        NotifyCommandsChanged();
    }

    private void NotifyCommandsChanged()
    {
        HitCommand.NotifyCanExecuteChanged();
        StandCommand.NotifyCanExecuteChanged();
        PeekCommand.NotifyCanExecuteChanged();
    }

    private void StartConfetti()
    {
        StopConfetti();
        // spawn initial burst
        for (int i = 0; i < 24; i++)
            Confetti.Add(CreateConfetto());

        _confettiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _confettiTimer.Tick += (_, _) =>
        {
            for (int i = Confetti.Count - 1; i >= 0; i--)
            {
                var c = Confetti[i];
                c.X += c.VX;
                c.Y += c.VY;
                c.VY += 0.25; // gravity
                c.Angle += c.VA;
                c.Opacity -= 0.02;
                if (c.Opacity <= 0 || c.Y > 800)
                    Confetti.RemoveAt(i);
            }
            // spawn a few more while fading
            if (Confetti.Count < 20 && _rng.NextDouble() < 0.3)
                Confetti.Add(CreateConfetto());
        };
        _confettiTimer.Start();
    }

    private void StopConfetti()
    {
        if (_confettiTimer is not null)
        {
            _confettiTimer.Stop();
            _confettiTimer = null;
        }
        Confetti.Clear();
    }

    private Confetto CreateConfetto()
    {
        double vx = (_rng.NextDouble() - 0.5) * 6.0;
        double vy = -(_rng.NextDouble() * 6 + 2);
        var brushes = new[] { Avalonia.Media.Brushes.Gold, Avalonia.Media.Brushes.Crimson, Avalonia.Media.Brushes.LimeGreen, Avalonia.Media.Brushes.DodgerBlue };
        return new Confetto
        {
            X = 160 + (_rng.NextDouble() - 0.5) * 200,
            Y = 0,
            VX = vx,
            VY = vy,
            Angle = _rng.NextDouble() * 360,
            VA = (_rng.NextDouble() - 0.5) * 10,
            Opacity = 1.0,
            Size = 6 + _rng.Next(10),
            Brush = brushes[_rng.Next(brushes.Length)]
        };
    }

    public class Confetto : ObservableObject
    {
        private double _x;
        private double _y;
        private double _vx;
        private double _vy;
        private double _angle;
        private double _va;
        private double _opacity;
        private double _size;
        private Avalonia.Media.IBrush? _brush;

        public double X { get => _x; set => SetProperty(ref _x, value); }
        public double Y { get => _y; set => SetProperty(ref _y, value); }
        public double VX { get => _vx; set => SetProperty(ref _vx, value); }
        public double VY { get => _vy; set => SetProperty(ref _vy, value); }
        public double Angle { get => _angle; set => SetProperty(ref _angle, value); }
        public double VA { get => _va; set => SetProperty(ref _va, value); }
        public double Opacity { get => _opacity; set => SetProperty(ref _opacity, value); }
        public double Size { get => _size; set => SetProperty(ref _size, value); }
        public Avalonia.Media.IBrush? Brush { get => _brush; set => SetProperty(ref _brush, value); }
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
        NotifyCommandsChanged();
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
