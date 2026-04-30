using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public abstract partial class GameViewModelBase : ViewModelBase
{
    public event Action? BackRequested;

    public abstract string Title { get; }
    public abstract string Description { get; }

    [ObservableProperty]
    private string _status = "Naciśnij \"Rozdaj\", aby zacząć.";

    [ObservableProperty]
    private string _player1Card = string.Empty;

    [ObservableProperty]
    private string _player2Card = string.Empty;

    [ObservableProperty]
    private int _player1DeckCount;

    [ObservableProperty]
    private int _player2DeckCount;

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Deal() => DealCore();

    protected abstract void DealCore();
}
