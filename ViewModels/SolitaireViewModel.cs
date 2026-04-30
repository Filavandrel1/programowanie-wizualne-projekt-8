using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using projekt8plsdzialaj.Models;

namespace projekt8plsdzialaj.ViewModels;

public partial class SolitaireViewModel : GameViewModelBase
{
    public override string Title => "Pasjans";
    public override string Description => "Klasyczny pasjans Klondike. Czerwone na czarne, malejąco.";
    public override string GameName => "Pasjans";

    private readonly Random _rng = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    private readonly List<SolitaireCard> _stockList = new();
    private SolitaireCard? _selectionAnchor;
    private List<SolitaireCard> _selectedRun = new();

    public ObservableCollection<TableauPile> Tableaus { get; } = new();
    public ObservableCollection<FoundationPile> Foundations { get; } = new();
    public ObservableCollection<SolitaireCard> Waste { get; } = new();

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _elapsedDisplay = "00:00";
    [ObservableProperty] private int _stockCount;
    [ObservableProperty] private SolitaireCard? _wasteTop;
    [ObservableProperty] private bool _isDealt;

    public SolitaireViewModel()
    {
        for (int i = 0; i < 7; i++)
            Tableaus.Add(new TableauPile { Index = i });
        foreach (var s in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
            Foundations.Add(new FoundationPile { Suit = s });

        _timer.Tick += (_, _) => ElapsedDisplay = FormatTime(_stopwatch.Elapsed);

        Status = "Naciśnij \"Rozdaj\", aby rozłożyć karty.";
    }

    // ---------- ROZDANIE ----------

    protected override void DealCore()
    {
        ClearSelection();
        Waste.Clear();
        _stockList.Clear();
        foreach (var t in Tableaus) t.Cards.Clear();
        foreach (var f in Foundations) { f.Cards.Clear(); f.RefreshDerived(); }

        var deck = BuildDeck();
        Shuffle(deck);

        // 1, 2, ... 7 kart w kolejnych kolumnach. Wierzch (ostatnia dołożona) odkryty.
        int idx = 0;
        for (int col = 0; col < 7; col++)
        {
            for (int row = 0; row <= col; row++)
            {
                var card = deck[idx++];
                card.IsFaceUp = (row == col);
                Tableaus[col].Cards.Add(card);
            }
        }
        // Pozostałe karty trafiają do stosu rezerwowego (zakryte).
        for (; idx < deck.Count; idx++)
        {
            deck[idx].IsFaceUp = false;
            _stockList.Add(deck[idx]);
        }

        RefreshAllVisuals();

        _stopwatch.Restart();
        _timer.Start();
        IsRunning = true;
        IsDealt = true;
        ElapsedDisplay = "00:00";
        Status = "Powodzenia! Czerwone na czarne, malejąco. As idzie na stos końcowy.";
        SolveCommand.NotifyCanExecuteChanged();
        SurrenderCommand.NotifyCanExecuteChanged();
    }

    private static List<SolitaireCard> BuildDeck()
    {
        var list = new List<SolitaireCard>(52);
        foreach (Suit s in Enum.GetValues<Suit>())
            for (int v = 1; v <= 13; v++)
                list.Add(new SolitaireCard { Suit = s, Value = v });
        return list;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ---------- KLIKNIĘCIA ----------

    [RelayCommand]
    private void ClickCard(SolitaireCard? card)
    {
        if (card is null || !IsRunning) return;

        var loc = FindCard(card);
        if (loc.Kind == LocationKind.None) return;

        // Karta zakryta – jeśli to wierzch kolumny roboczej, odsłoń.
        if (!card.IsFaceUp)
        {
            if (loc.Kind == LocationKind.Tableau &&
                Tableaus[loc.Index].Cards[^1] == card)
            {
                card.IsFaceUp = true;
                ClearSelection();
                RefreshAllVisuals();
            }
            return;
        }

        // Mamy zaznaczenie i kliknęliśmy na cel:
        if (_selectionAnchor is not null)
        {
            // Kliknięcie tej samej karty – odznacz.
            if (_selectionAnchor == card)
            {
                ClearSelection();
                return;
            }

            if (loc.Kind == LocationKind.Tableau)
            {
                if (TryMoveSelectionToTableau(loc.Index))
                    return;
            }
            else if (loc.Kind == LocationKind.Foundation)
            {
                if (TryMoveSelectionToFoundation(loc.Index))
                    return;
            }
            // Cel niewłaściwy – zmień zaznaczenie na nowo klikniętą kartę.
        }

        SelectCard(card, loc);
    }

    [RelayCommand]
    private void ClickPile(TableauPile? pile)
    {
        if (pile is null || !IsRunning) return;
        // Kliknięcie pustej kolumny – próbujemy przenieść tam zaznaczenie (musi zaczynać się od K).
        if (_selectionAnchor is null) return;
        if (pile.Cards.Count > 0) return; // niepusta – kliknięto kartę, nie pustą część
        TryMoveSelectionToTableau(pile.Index);
    }

    [RelayCommand]
    private void ClickFoundation(FoundationPile? f)
    {
        if (f is null || !IsRunning) return;
        var idx = Foundations.IndexOf(f);
        if (idx < 0) return;

        if (_selectionAnchor is null)
        {
            // Brak zaznaczenia – jeśli na stosie końcowym leży karta, weź ją (można cofnąć).
            if (f.Cards.Count == 0) return;
            var top = f.Cards[^1];
            SelectCard(top, new CardLocation { Kind = LocationKind.Foundation, Index = idx });
            return;
        }

        // Kliknięcie tego samego stosu z którego pochodzi zaznaczenie – odznacz.
        var srcLoc = FindCard(_selectionAnchor);
        if (srcLoc.Kind == LocationKind.Foundation && srcLoc.Index == idx)
        {
            ClearSelection();
            return;
        }

        TryMoveSelectionToFoundation(idx);
    }

    [RelayCommand]
    private void ClickStock()
    {
        if (!IsRunning) return;
        ClearSelection();

        if (_stockList.Count > 0)
        {
            // Bierzemy wierzch stosu rezerwowego (ostatni element listy) i odkrywamy do "Waste".
            var card = _stockList[^1];
            _stockList.RemoveAt(_stockList.Count - 1);
            card.IsFaceUp = true;
            Waste.Add(card);
        }
        else if (Waste.Count > 0)
        {
            // Recykling: wszystkie karty z waste wracają do stocka, zakryte.
            for (int i = Waste.Count - 1; i >= 0; i--)
            {
                var c = Waste[i];
                c.IsFaceUp = false;
                _stockList.Add(c);
            }
            Waste.Clear();
        }

        RefreshAllVisuals();
    }

    // ---------- ZAZNACZANIE ----------

    private void SelectCard(SolitaireCard card, CardLocation loc)
    {
        ClearSelection();

        if (loc.Kind == LocationKind.Tableau)
        {
            var pile = Tableaus[loc.Index].Cards;
            int from = pile.IndexOf(card);
            for (int i = from; i < pile.Count; i++)
            {
                _selectedRun.Add(pile[i]);
                pile[i].IsSelected = true;
            }
        }
        else if (loc.Kind == LocationKind.Waste)
        {
            // Z waste wybieramy tylko wierzchnią.
            if (Waste.Count == 0 || Waste[^1] != card) return;
            _selectedRun.Add(card);
            card.IsSelected = true;
        }
        else if (loc.Kind == LocationKind.Foundation)
        {
            // Z foundation – tylko wierzchnia, jako pojedyncza karta.
            var f = Foundations[loc.Index];
            if (f.Cards.Count == 0 || f.Cards[^1] != card) return;
            _selectedRun.Add(card);
            card.IsSelected = true;
        }
        else return;

        _selectionAnchor = card;
    }

    private void ClearSelection()
    {
        foreach (var c in _selectedRun) c.IsSelected = false;
        _selectedRun = new();
        _selectionAnchor = null;
    }

    // ---------- WALIDACJA I RUCH ----------

    private bool TryMoveSelectionToTableau(int targetIndex)
    {
        if (_selectionAnchor is null) return false;
        var target = Tableaus[targetIndex];
        var bottom = _selectedRun[0]; // najniższa karta przenoszonego ciągu

        bool valid;
        if (target.Cards.Count == 0)
        {
            // Pusta kolumna – akceptujemy dowolną kartę (uproszczona zasada).
            valid = true;
        }
        else
        {
            var top = target.Cards[^1];
            if (!top.IsFaceUp) return false;
            // Naprzemienne kolory + malejąco o 1.
            valid = top.IsRed != bottom.IsRed && top.Value == bottom.Value + 1;
        }

        if (!valid) return false;

        // Wykonaj przeniesienie.
        var sourceLoc = FindCard(_selectionAnchor);
        RemoveSelectionFromSource(sourceLoc);

        foreach (var c in _selectedRun)
            target.Cards.Add(c);

        FinalizeMove(sourceLoc);
        return true;
    }

    private bool TryMoveSelectionToFoundation(int foundationIndex)
    {
        if (_selectionAnchor is null) return false;
        if (_selectedRun.Count != 1) return false; // na foundation – tylko pojedyncza karta

        var card = _selectedRun[0];
        var f = Foundations[foundationIndex];
        if (f.Suit != card.Suit) return false;

        bool valid = f.Cards.Count == 0
            ? card.Value == 1
            : f.Cards[^1].Value + 1 == card.Value;

        if (!valid) return false;

        var sourceLoc = FindCard(card);
        RemoveSelectionFromSource(sourceLoc);

        f.Cards.Add(card);
        f.RefreshDerived();

        FinalizeMove(sourceLoc);
        CheckWin();
        return true;
    }

    private void RemoveSelectionFromSource(CardLocation src)
    {
        switch (src.Kind)
        {
            case LocationKind.Tableau:
                var pile = Tableaus[src.Index].Cards;
                for (int i = 0; i < _selectedRun.Count; i++)
                    pile.RemoveAt(pile.Count - 1);
                break;
            case LocationKind.Waste:
                Waste.RemoveAt(Waste.Count - 1);
                break;
            case LocationKind.Foundation:
                var fp = Foundations[src.Index];
                fp.Cards.RemoveAt(fp.Cards.Count - 1);
                fp.RefreshDerived();
                break;
        }
    }

    private void FinalizeMove(CardLocation src)
    {
        // Po ruchu z kolumny: jeśli nowy wierzch jest zakryty, odsłoń go.
        if (src.Kind == LocationKind.Tableau)
        {
            var pile = Tableaus[src.Index].Cards;
            if (pile.Count > 0 && !pile[^1].IsFaceUp)
                pile[^1].IsFaceUp = true;
        }

        ClearSelection();
        RefreshAllVisuals();
    }

    private void CheckWin()
    {
        if (Foundations.All(f => f.Cards.Count == 13))
        {
            _stopwatch.Stop();
            _timer.Stop();
            IsRunning = false;
            ElapsedDisplay = FormatTime(_stopwatch.Elapsed);
            Status = $"🎉 Brawo! Ułożyłeś pasjansa w czasie {ElapsedDisplay}.";
            RecordResult($"ułożono w {ElapsedDisplay}");
            SolveCommand.NotifyCanExecuteChanged();
            SurrenderCommand.NotifyCanExecuteChanged();
        }
    }

    // ---------- LOKALIZACJA KARTY ----------

    private enum LocationKind { None, Tableau, Waste, Foundation }
    private readonly struct CardLocation
    {
        public LocationKind Kind { get; init; }
        public int Index { get; init; }
    }

    private CardLocation FindCard(SolitaireCard card)
    {
        for (int i = 0; i < Tableaus.Count; i++)
            if (Tableaus[i].Cards.Contains(card))
                return new CardLocation { Kind = LocationKind.Tableau, Index = i };
        if (Waste.Contains(card))
            return new CardLocation { Kind = LocationKind.Waste, Index = 0 };
        for (int i = 0; i < Foundations.Count; i++)
            if (Foundations[i].Cards.Contains(card))
                return new CardLocation { Kind = LocationKind.Foundation, Index = i };
        return new CardLocation { Kind = LocationKind.None, Index = -1 };
    }

    // ---------- ODŚWIEŻANIE WIDOKU ----------

    private void RefreshAllVisuals()
    {
        // Wertykalne odsunięcie kart w kolumnach (pierwsza karta bez marginesu, kolejne -65px).
        foreach (var t in Tableaus)
        {
            for (int i = 0; i < t.Cards.Count; i++)
                t.Cards[i].StackMargin = i == 0
                    ? new Thickness(0)
                    : new Thickness(0, t.Cards[i - 1].IsFaceUp ? -78 : -88, 0, 0);
        }

        StockCount = _stockList.Count;
        WasteTop = Waste.Count > 0 ? Waste[^1] : null;
        foreach (var f in Foundations) f.RefreshDerived();
    }

    // ---------- KOŃCZENIE PARTII ----------

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Solve()
    {
        StopClock();
        Status = $"Ułożyłeś pasjansa w czasie {ElapsedDisplay}.";
        RecordResult($"ułożono w {ElapsedDisplay}");
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Surrender()
    {
        StopClock();
        Status = "Poddałeś się – pasjans nie został ułożony.";
        RecordResult("poddanie");
    }

    private void StopClock()
    {
        _stopwatch.Stop();
        _timer.Stop();
        IsRunning = false;
        ElapsedDisplay = FormatTime(_stopwatch.Elapsed);
        SolveCommand.NotifyCanExecuteChanged();
        SurrenderCommand.NotifyCanExecuteChanged();
    }

    private bool CanFinish() => IsRunning;

    partial void OnIsRunningChanged(bool value)
    {
        SolveCommand.NotifyCanExecuteChanged();
        SurrenderCommand.NotifyCanExecuteChanged();
    }

    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes:D2}:{t.Seconds:D2}";
}
