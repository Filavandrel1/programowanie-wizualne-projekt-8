using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public partial class HiLoViewModel : GameViewModelBase
{
    public override string Title => "?!";
    public override string Description => "Sekretna gra dla odważnych.";
    public override string GameName => "DiabelskaTalia";

    private readonly Random _rng = new();
    private readonly List<int> _deck = new();
    private int _deckIndex;
    private readonly DispatcherTimer _animTimer;
    private double _t;

    public const int MaxMistakes = 10;

    // Faktyczna karta (logika).
    private int _currentValue = 1;
    private int _currentSuitIndex; // 0 ♠, 1 ♣, 2 ♥, 3 ♦
    private bool _currentRealIsRed;

    // Karta wyświetlana (zniekształcona w M4/M6).
    [ObservableProperty] private string _displayedRank = "?";
    [ObservableProperty] private string _displayedSuit = "?";
    [ObservableProperty] private bool _displayedIsRed;

    [ObservableProperty] private int _mistakeCount;
    [ObservableProperty] private int _score;
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private string _resultText = string.Empty;

    // ---------- ETAPY ----------
    public bool M1 => MistakeCount >= 1; // trzęsienie ekranu + 3 karty (boczne rozmyte)
    public bool M2 => MistakeCount >= 2; // znaczne zwiększenie trzęsienia
    public bool M3 => MistakeCount >= 3; // ♥↔♦, ♣↔♠ migotanie
    public bool M4 => MistakeCount >= 4; // tęczowe kolory karty + tło
    public bool M5 => MistakeCount >= 5; // cała karta rozmazana, ±1
    public bool M6 => MistakeCount >= 6; // jeszcze więcej kolorów (rozszerzona paleta + szybciej + intensywniej)
    public bool M7 => MistakeCount >= 7; // poruszające się rozmyte mroczki
    public bool M8 => MistakeCount >= 8; // dodatkowe karty w różnych miejscach ekranu
    public bool M9 => MistakeCount >= 9; // czarny ekran z losowymi przerwami

    public bool ShowThreeCards => M1;

    // ---------- ANIMACJE ----------
    [ObservableProperty] private double _screenSwayAngle;
    [ObservableProperty] private double _screenOffsetX;
    [ObservableProperty] private double _screenOffsetY;

    [ObservableProperty] private double _card1SwayAngle;
    [ObservableProperty] private double _card1OffsetX;
    [ObservableProperty] private double _card1OffsetY;
    [ObservableProperty] private double _card2SwayAngle;
    [ObservableProperty] private double _card2OffsetX;
    [ObservableProperty] private double _card2OffsetY;
    [ObservableProperty] private double _card3SwayAngle;
    [ObservableProperty] private double _card3OffsetX;
    [ObservableProperty] private double _card3OffsetY;

    // Blur osobno na karty boczne (M2+) i na całość kart (M6+).
    [ObservableProperty] private double _sideCardBlur;
    [ObservableProperty] private double _allCardsBlur;

    // Kolor karty (od M5 zmienia się tęczowo, wcześniej czarny/czerwony).
    [ObservableProperty] private Color _displayedColor = Colors.Black;

    // Tło (od M5: migoczące kolory zamiast standardowego ciemnego tła).
    [ObservableProperty] private Color _backgroundColor = Color.FromRgb(0x1A, 0x06, 0x06);

    // Plamy M7+ – pozycja jako Thickness (top/left).
    [ObservableProperty] private Thickness _spot1Margin;
    [ObservableProperty] private Thickness _spot2Margin;
    [ObservableProperty] private Thickness _spot3Margin;
    [ObservableProperty] private Thickness _spot4Margin;
    [ObservableProperty] private Thickness _spot5Margin;

    // Dodatkowe karty M8+ (4 karty w narożnikach z własnym chwianiem).
    [ObservableProperty] private double _extraCard1SwayAngle;
    [ObservableProperty] private double _extraCard1OffsetX;
    [ObservableProperty] private double _extraCard1OffsetY;
    [ObservableProperty] private double _extraCard2SwayAngle;
    [ObservableProperty] private double _extraCard2OffsetX;
    [ObservableProperty] private double _extraCard2OffsetY;
    [ObservableProperty] private double _extraCard3SwayAngle;
    [ObservableProperty] private double _extraCard3OffsetX;
    [ObservableProperty] private double _extraCard3OffsetY;
    [ObservableProperty] private double _extraCard4SwayAngle;
    [ObservableProperty] private double _extraCard4OffsetX;
    [ObservableProperty] private double _extraCard4OffsetY;

    [ObservableProperty] private double _rankBlur;
    [ObservableProperty] private double _blackoutOpacity;

    // Stan losowych przerw blackoutu (M9).
    private bool _blackoutVisible;
    private double _nextBlackoutSwitch;

    partial void OnMistakeCountChanged(int value)
    {
        OnPropertyChanged(nameof(M1));
        OnPropertyChanged(nameof(M2));
        OnPropertyChanged(nameof(M3));
        OnPropertyChanged(nameof(M4));
        OnPropertyChanged(nameof(M5));
        OnPropertyChanged(nameof(M6));
        OnPropertyChanged(nameof(M7));
        OnPropertyChanged(nameof(M8));
        OnPropertyChanged(nameof(M9));
        OnPropertyChanged(nameof(ShowThreeCards));
    }

    public HiLoViewModel()
    {
        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _animTimer.Tick += OnTick;
        _animTimer.Start();
        BackRequested += StopAnim;
        DealCore();
    }

    private void StopAnim()
    {
        _animTimer.Stop();
        _animTimer.Tick -= OnTick;
    }

    protected override void DealCore()
    {
        _deck.Clear();
        for (int s = 0; s < 4; s++)
            for (int v = 1; v <= 13; v++)
                _deck.Add(s * 100 + v);
        Shuffle(_deck);
        _deckIndex = 0;
        MistakeCount = 0;
        Score = 0;
        IsGameOver = false;
        ResultText = string.Empty;
        DrawNext();
        Status = "Czy następna karta będzie WIĘKSZA czy MNIEJSZA?";
    }

    private void DrawNext()
    {
        if (_deckIndex >= _deck.Count) { Shuffle(_deck); _deckIndex = 0; }
        ApplyCard(_deck[_deckIndex++]);
    }

    private void ApplyCard(int code)
    {
        _currentSuitIndex = code / 100;
        _currentValue = code % 100;
        _currentRealIsRed = _currentSuitIndex >= 2;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [RelayCommand] private void GuessHigher() => Guess(true);
    [RelayCommand] private void GuessLower() => Guess(false);

    private void Guess(bool higher)
    {
        if (IsGameOver) return;
        int prev = _currentValue;
        DrawNext();
        int now = _currentValue;

        bool correct;
        if (now == prev) correct = false;
        else correct = higher ? now > prev : now < prev;

        if (correct)
        {
            Score++;
            Status = $"Dobrze! Punkty: {Score}, pomyłki: {MistakeCount}/{MaxMistakes}";
        }
        else
        {
            MistakeCount++;
            if (MistakeCount >= MaxMistakes)
            {
                IsGameOver = true;
                ResultText = $"Gra skończona. Twój wynik: {Score} pkt.";
                Status = ResultText;
                RecordResult($"{Score} pkt (przegrana)");
            }
            else
            {
                Status = $"Błąd! Pomyłki: {MistakeCount}/{MaxMistakes}. Coś jest nie tak...";
            }
        }
    }

    [RelayCommand]
    private void Restart() => DealCore();

    private static readonly Color[] _rainbow = new[]
    {
        Color.FromRgb(0xC0, 0x20, 0x2A), // czerwony
        Color.FromRgb(0xF5, 0xC0, 0x18), // żółty
        Color.FromRgb(0x1F, 0xAE, 0x6B), // zielony
        Color.FromRgb(0x36, 0x66, 0xC8), // niebieski
        Color.FromRgb(0x9B, 0x6B, 0xD8), // fiolet
        Color.FromRgb(0xFF, 0x6F, 0x1F), // pomarańcz
        Color.FromRgb(0x14, 0xC8, 0xC8), // cyjan
        Color.FromRgb(0xE0, 0x40, 0xA0), // róż
    };

    // Rozszerzona, jaskrawa paleta dla M6 — szybsze, intensywniejsze migotanie.
    private static readonly Color[] _rainbowExtended = new[]
    {
        Color.FromRgb(0xFF, 0x00, 0x33),
        Color.FromRgb(0xFF, 0x55, 0x00),
        Color.FromRgb(0xFF, 0xAA, 0x00),
        Color.FromRgb(0xFF, 0xEE, 0x00),
        Color.FromRgb(0xC8, 0xFF, 0x00),
        Color.FromRgb(0x55, 0xFF, 0x22),
        Color.FromRgb(0x00, 0xFF, 0x88),
        Color.FromRgb(0x00, 0xFF, 0xEE),
        Color.FromRgb(0x00, 0xCC, 0xFF),
        Color.FromRgb(0x33, 0x66, 0xFF),
        Color.FromRgb(0x77, 0x33, 0xFF),
        Color.FromRgb(0xCC, 0x22, 0xFF),
        Color.FromRgb(0xFF, 0x22, 0xDD),
        Color.FromRgb(0xFF, 0x66, 0xAA),
        Color.FromRgb(0xFF, 0xAA, 0x66),
        Color.FromRgb(0xCC, 0xFF, 0xCC),
    };

    private void OnTick(object? s, EventArgs e)
    {
        _t += 0.05;

        // Trzęsienie eskaluje od M1 (lekkie) przez M2 (mocne) po M5 (skrajne).
        double shake = M5 ? 4.0 : M2 ? 2.2 : M1 ? 1.0 : 0.0;

        // ekran
        ScreenSwayAngle = M1 ? Math.Sin(_t * 1.7) * (1.2 * shake) : 0;
        ScreenOffsetX = M1 ? Math.Sin(_t * 2.3) * (3 * shake) : 0;
        ScreenOffsetY = M1 ? Math.Cos(_t * 2.0) * (2 * shake) : 0;

        // 3 karty osobno
        if (M1)
        {
            Card1SwayAngle = Math.Sin(_t * 2.6 + 0.0) * (3 * shake);
            Card1OffsetX = Math.Sin(_t * 3.1 + 0.0) * (4 * shake);
            Card1OffsetY = Math.Cos(_t * 2.7 + 0.0) * (3 * shake);

            Card2SwayAngle = Math.Sin(_t * 2.1 + 1.7) * (3 * shake);
            Card2OffsetX = Math.Sin(_t * 3.4 + 1.7) * (4 * shake);
            Card2OffsetY = Math.Cos(_t * 2.4 + 1.7) * (3 * shake);

            Card3SwayAngle = Math.Sin(_t * 3.0 + 3.4) * (3 * shake);
            Card3OffsetX = Math.Sin(_t * 2.8 + 3.4) * (4 * shake);
            Card3OffsetY = Math.Cos(_t * 3.3 + 3.4) * (3 * shake);
        }
        else
        {
            Card1SwayAngle = Card2SwayAngle = Card3SwayAngle = 0;
            Card1OffsetX = Card2OffsetX = Card3OffsetX = 0;
            Card1OffsetY = Card2OffsetY = Card3OffsetY = 0;
        }

        // M1: rozmycie kart bocznych (boczne pojawiają się razem z M1).
        SideCardBlur = M1 ? 6.0 : 0.0;

        // M3: ♥↔♦, ♣↔♠ – szybkie migotanie symbolu (kolor zachowany).
        int displaySuit = _currentSuitIndex;
        if (M3)
        {
            bool flip = ((int)Math.Floor(_t * 6) & 1) == 1;
            displaySuit = _currentRealIsRed
                ? (flip ? 2 : 3)
                : (flip ? 0 : 1);
        }
        DisplayedSuit = displaySuit switch { 0 => "♠", 1 => "♣", 2 => "♥", _ => "♦" };
        DisplayedIsRed = displaySuit >= 2;

        // M6: jeszcze więcej kolorów — rozszerzona paleta, szybciej, mocniej.
        // M4: tęczowe kolory karty + tła (bazowa paleta).
        if (M6)
        {
            int idx = ((int)Math.Floor(_t * 11)) % _rainbowExtended.Length;
            DisplayedColor = _rainbowExtended[idx];
            int bgIdx = ((int)Math.Floor(_t * 9) + 5) % _rainbowExtended.Length;
            var c = _rainbowExtended[bgIdx];
            // Mocniejsza saturacja tła niż w M4.
            BackgroundColor = Color.FromRgb((byte)(c.R * 0.75), (byte)(c.G * 0.75), (byte)(c.B * 0.75));
        }
        else if (M4)
        {
            int idx = ((int)Math.Floor(_t * 7)) % _rainbow.Length;
            DisplayedColor = _rainbow[idx];
            int bgIdx = ((int)Math.Floor(_t * 5) + 3) % _rainbow.Length;
            var c = _rainbow[bgIdx];
            BackgroundColor = Color.FromRgb((byte)(c.R * 0.55), (byte)(c.G * 0.55), (byte)(c.B * 0.55));
        }
        else
        {
            DisplayedColor = DisplayedIsRed
                ? Color.FromRgb(0xC0, 0x20, 0x2A)
                : Color.FromRgb(0x10, 0x10, 0x10);
            BackgroundColor = Color.FromRgb(0x1A, 0x06, 0x06);
        }

        // M5: cała karta rozmazuje się, blur ciągle się zmienia, liczba skacze ±1.
        int displayValue = _currentValue;
        if (M5)
        {
            int delta = ((int)Math.Floor(_t * 8) % 3) - 1; // -1, 0, +1
            displayValue = Math.Clamp(_currentValue + delta, 1, 13);
            RankBlur = 6 + 4 * Math.Sin(_t * 4) + 3 * Math.Sin(_t * 1.7 + 0.7);
            AllCardsBlur = 5 + 3 * Math.Sin(_t * 2.3) + 2 * Math.Cos(_t * 3.7);
        }
        else
        {
            RankBlur = 0;
            AllCardsBlur = 0;
        }
        DisplayedRank = displayValue switch
        {
            1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => displayValue.ToString()
        };

        // M7: poruszające się czarne plamy (każda inny tor).
        if (M7)
        {
            Spot1Margin = new Thickness(60  + Math.Sin(_t * 0.7 + 0.0) * 80, 80  + Math.Cos(_t * 0.6 + 0.0) * 60, 0, 0);
            Spot2Margin = new Thickness(520 + Math.Sin(_t * 0.5 + 1.2) * 100, 40  + Math.Cos(_t * 0.9 + 1.2) * 70, 0, 0);
            Spot3Margin = new Thickness(320 + Math.Sin(_t * 0.8 + 2.4) * 90, 380 + Math.Cos(_t * 0.7 + 2.4) * 80, 0, 0);
            Spot4Margin = new Thickness(40  + Math.Sin(_t * 0.6 + 3.6) * 110, 440 + Math.Cos(_t * 0.5 + 3.6) * 60, 0, 0);
            Spot5Margin = new Thickness(640 + Math.Sin(_t * 0.9 + 4.8) * 90, 320 + Math.Cos(_t * 0.8 + 4.8) * 80, 0, 0);
        }

        // M8: dodatkowe karty rozsiane po 4 rogach ekranu, każda z własnym kołysaniem.
        if (M8)
        {
            // Lewy-górny
            ExtraCard1OffsetX = -360 + Math.Sin(_t * 1.9 + 0.3) * 30;
            ExtraCard1OffsetY = -210 + Math.Cos(_t * 1.7 + 0.3) * 25;
            ExtraCard1SwayAngle = Math.Sin(_t * 2.4 + 0.3) * 8;
            // Prawy-górny
            ExtraCard2OffsetX =  360 + Math.Sin(_t * 1.6 + 1.5) * 30;
            ExtraCard2OffsetY = -210 + Math.Cos(_t * 2.1 + 1.5) * 25;
            ExtraCard2SwayAngle = Math.Sin(_t * 2.0 + 1.5) * 8;
            // Lewy-dolny
            ExtraCard3OffsetX = -360 + Math.Sin(_t * 2.2 + 2.7) * 30;
            ExtraCard3OffsetY =  210 + Math.Cos(_t * 1.8 + 2.7) * 25;
            ExtraCard3SwayAngle = Math.Sin(_t * 2.7 + 2.7) * 8;
            // Prawy-dolny
            ExtraCard4OffsetX =  360 + Math.Sin(_t * 1.7 + 4.1) * 30;
            ExtraCard4OffsetY =  210 + Math.Cos(_t * 2.3 + 4.1) * 25;
            ExtraCard4SwayAngle = Math.Sin(_t * 2.2 + 4.1) * 8;
        }

        // M9: czarny ekran z LOSOWYMI przerwami — długie czarne (2.0–4.5 s),
        // krótkie czytelne okna (0.5–1.2 s), żeby od czasu do czasu dało się odczytać karty.
        if (M9)
        {
            if (_t >= _nextBlackoutSwitch)
            {
                _blackoutVisible = !_blackoutVisible;
                double dur = _blackoutVisible
                    ? 2.0 + _rng.NextDouble() * 2.5    // czarne 2.0–4.5 s
                    : 0.5 + _rng.NextDouble() * 0.7;   // widoczne 0.5–1.2 s
                _nextBlackoutSwitch = _t + dur;
            }
            BlackoutOpacity = _blackoutVisible ? 1.0 : 0.0;
        }
        else
        {
            _blackoutVisible = false;
            _nextBlackoutSwitch = 0;
            BlackoutOpacity = 0.0;
        }
    }
}
