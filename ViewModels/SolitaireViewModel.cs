using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public partial class SolitaireViewModel : GameViewModelBase
{
    public override string Title => "Pasjans";
    public override string Description => "Ułóż karty jak najszybciej. Czas leci od momentu rozdania.";
    public override string GameName => "Pasjans";

    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _elapsedDisplay = "00:00";

    public SolitaireViewModel()
    {
        Status = "Naciśnij \"Rozdaj\", aby rozłożyć karty i wystartować zegar.";
    }

    protected override void DealCore()
    {
        _stopwatch.Restart();
        IsRunning = true;
        ElapsedDisplay = "00:00";
        Status = "Karty rozłożone. Powodzenia! Kliknij „Ułożone!”, gdy skończysz.";
        SolveCommand.NotifyCanExecuteChanged();
        SurrenderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Solve()
    {
        _stopwatch.Stop();
        IsRunning = false;
        var elapsed = _stopwatch.Elapsed;
        var formatted = FormatTime(elapsed);
        ElapsedDisplay = formatted;
        Status = $"Brawo! Ułożyłeś pasjansa w czasie {formatted}.";
        RecordResult($"ułożono w {formatted}");
        SolveCommand.NotifyCanExecuteChanged();
        SurrenderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Surrender()
    {
        _stopwatch.Stop();
        IsRunning = false;
        Status = "Poddałeś się – pasjans nie został ułożony.";
        RecordResult("poddanie");
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
