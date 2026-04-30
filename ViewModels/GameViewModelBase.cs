using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using projekt8plsdzialaj.Models;

namespace projekt8plsdzialaj.ViewModels;

public abstract partial class GameViewModelBase : ViewModelBase
{
    public event Action? BackRequested;

    public abstract string Title { get; }
    public abstract string Description { get; }

    /// <summary>
    /// Nazwa używana w historii rozgrywek (krótka, np. "Wojna").
    /// </summary>
    public abstract string GameName { get; }

    [ObservableProperty]
    private string _playerName = "Gość";

    [ObservableProperty]
    private string _status = "Naciśnij \"Rozdaj\", aby zacząć.";

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Deal() => DealCore();

    protected abstract void DealCore();

    /// <summary>
    /// Zapisuje rezultat rozgrywki do historii.
    /// </summary>
    protected void RecordResult(string result)
    {
        GameHistoryStore.Add(new GameRecord
        {
            PlayerName = string.IsNullOrWhiteSpace(PlayerName) ? "Gość" : PlayerName,
            GameName = GameName,
            Result = result,
            PlayedAt = DateTime.Now
        });
    }
}
