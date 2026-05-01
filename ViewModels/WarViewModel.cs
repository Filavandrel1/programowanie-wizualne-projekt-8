using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace projekt8plsdzialaj.ViewModels;

public partial class WarViewModel : GameViewModelBase
{
    public override string Title => "Wojna";
    public override string Description => "Rozgrywka przeciwko komputerowi — wyższa karta wygrywa rundę.";
    public override string GameName => "Wojna";
    public override bool CanSurrender => true;

    private const string PlayerLabel = "Ty";
    private const string ComputerLabel = "Komputer";

    private static readonly string[] Ranks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };
    private readonly Random _rng = new();
    private readonly Queue<string> _player1Deck = new();
    private readonly Queue<string> _player2Deck = new();

    [ObservableProperty]
    private string _player1Card = string.Empty;

    [ObservableProperty]
    private string _player2Card = string.Empty;

    [ObservableProperty]
    private int _player1DeckCount;

    [ObservableProperty]
    private int _player2DeckCount;

    public string Player1Name => PlayerLabel;
    public string Player2Name => ComputerLabel;

    public WarViewModel()
    {
        InitializeDecks();
    }

    private void InitializeDecks()
    {
        var deck = new List<string>();

        foreach (var rank in Ranks)
        {
            foreach (var suit in Suits)
            {
                deck.Add($"{rank}{suit}");
            }
        }

        Shuffle(deck);

        _player1Deck.Clear();
        _player2Deck.Clear();

        for (int i = 0; i < deck.Count; i++)
        {
            if (i % 2 == 0)
            {
                _player1Deck.Enqueue(deck[i]);
            }
            else
            {
                _player2Deck.Enqueue(deck[i]);
            }
        }

        Player1Card = _player1Deck.Count > 0 ? _player1Deck.Peek() : string.Empty;
        Player2Card = _player2Deck.Count > 0 ? _player2Deck.Peek() : string.Empty;
        UpdateDeckCounts();
        Status = "Talia przygotowana. Rozpocznij rundę klikając Rozdaj.";
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    protected override void DealCore()
    {
        if (_player1Deck.Count == 0 || _player2Deck.Count == 0)
        {
            Status = _player1Deck.Count == 0 && _player2Deck.Count == 0
                ? "Koniec gry — obie talie są puste."
                : _player1Deck.Count == 0
                    ? "Koniec gry — wygrał Komputer!"
                    : "Koniec gry — wygrałeś!";
            RecordResult(_player1Deck.Count == 0 ? "przegrana" : "wygrana");
            return;
        }

        var spoils = new List<string>();
        var winner = ResolveBattle(spoils, out var roundSummary);

        if (winner == 1)
        {
            foreach (var card in spoils)
            {
                _player1Deck.Enqueue(card);
            }
        }
        else if (winner == 2)
        {
            foreach (var card in spoils)
            {
                _player2Deck.Enqueue(card);
            }
        }

        UpdateDeckCounts();
        Status = roundSummary +
                 $"\nKarty w talii: {_player1Deck.Count} vs {_player2Deck.Count}";

        if (_player1Deck.Count == 0 || _player2Deck.Count == 0)
        {
            bool humanWon = _player2Deck.Count == 0;
            Status += humanWon
                ? "\nKoniec gry — wygrałeś!"
                : "\nKoniec gry — wygrał Komputer!";
            RecordResult(humanWon ? "wygrana" : "przegrana");
        }
    }

    private int ResolveBattle(List<string> spoils, out string summary)
    {
        var p1Card = _player1Deck.Dequeue();
        var p2Card = _player2Deck.Dequeue();

        Player1Card = p1Card;
        Player2Card = p2Card;

        spoils.Add(p1Card);
        spoils.Add(p2Card);

        var p1Value = GetRankValue(p1Card);
        var p2Value = GetRankValue(p2Card);

        if (p1Value > p2Value)
        {
            summary = $"Ty: {p1Card}   |   Komputer: {p2Card}\nWygrałeś rundę!";
            return 1;
        }

        if (p2Value > p1Value)
        {
            summary = $"Ty: {p1Card}   |   Komputer: {p2Card}\nKomputer wygrał rundę.";
            return 2;
        }

        summary = $"Ty: {p1Card}   |   Komputer: {p2Card}\nRemis — wojna!";
        var warWinner = ResolveWar(spoils, out var warSummary);
        summary += "\n" + warSummary;
        return warWinner;
    }

    private int ResolveWar(List<string> spoils, out string summary)
    {
        if (_player1Deck.Count == 0 || _player2Deck.Count == 0)
        {
            summary = "Nie ma wystarczającej liczby kart do kontynuacji wojny.";
            return _player1Deck.Count > 0 ? 1 : 2;
        }

        int faceDownCount1 = Math.Min(3, Math.Max(0, _player1Deck.Count - 1));
        int faceDownCount2 = Math.Min(3, Math.Max(0, _player2Deck.Count - 1));

        DrawWarCards(_player1Deck, spoils, faceDownCount1);
        DrawWarCards(_player2Deck, spoils, faceDownCount2);

        if (_player1Deck.Count == 0 || _player2Deck.Count == 0)
        {
            summary = "Jeden z graczy nie ma już karty do wojny.";
            return _player1Deck.Count > 0 ? 1 : 2;
        }

        var p1Face = _player1Deck.Dequeue();
        var p2Face = _player2Deck.Dequeue();

        Player1Card = p1Face;
        Player2Card = p2Face;

        spoils.Add(p1Face);
        spoils.Add(p2Face);

        var p1Value = GetRankValue(p1Face);
        var p2Value = GetRankValue(p2Face);

        summary = $"Wojna! Ty kładziesz {faceDownCount1} zakrytą kartę{(faceDownCount1 == 1 ? "" : "y")}, " +
                  $"Komputer kładzie {faceDownCount2} zakryte kart{(faceDownCount2 == 1 ? "" : "y")}." +
                  $" Ty: {p1Face}   |   Komputer: {p2Face}";

        if (p1Value > p2Value)
        {
            summary += "\nWygrałeś rundę!";
            return 1;
        }

        if (p2Value > p1Value)
        {
            summary += "\nKomputer wygrał rundę.";
            return 2;
        }

        summary += "\nRemis — kolejna wojna!";
        var nestedWinner = ResolveWar(spoils, out var nestedSummary);
        summary += "\n" + nestedSummary;
        return nestedWinner;
    }

    private static void DrawWarCards(Queue<string> deck, List<string> spoils, int count)
    {
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            spoils.Add(deck.Dequeue());
        }
    }

    private void UpdateDeckCounts()
    {
        Player1DeckCount = _player1Deck.Count;
        Player2DeckCount = _player2Deck.Count;
    }

    private static int GetRankValue(string card)
    {
        var rank = card.Substring(0, card.Length - 1);
        return Array.IndexOf(Ranks, rank);
    }

    protected override void SurrenderCore()
    {
        RecordResult("poddanie");
        Status = "Poddano się — koniec gry. Komputer wygrał.";
    }
}
