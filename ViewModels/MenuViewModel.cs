using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public partial class MenuViewModel : ViewModelBase
{
    public event Action<ViewModelBase>? GameSelected;
    public event Action? HistoryRequested;

    private Func<string, GameViewModelBase>? _pendingGameFactory;

    [ObservableProperty] private bool _isLoginVisible;
    [ObservableProperty] private string _pendingGameTitle = string.Empty;
    [ObservableProperty] private string _playerNameInput = string.Empty;

    partial void OnPlayerNameInputChanged(string value)
        => ConfirmLoginCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void StartWar() => RequestLogin("Wojna", name => new WarViewModel { PlayerName = name });

    [RelayCommand]
    private void StartBlackjack() => RequestLogin("Oczko", name => new BlackjackViewModel { PlayerName = name });

    [RelayCommand]
    private void StartSolitaire() => RequestLogin("Pasjans", name => new SolitaireViewModel { PlayerName = name });

    [RelayCommand]
    private void ShowHistory() => HistoryRequested?.Invoke();

    private void RequestLogin(string gameTitle, Func<string, GameViewModelBase> factory)
    {
        PendingGameTitle = gameTitle;
        _pendingGameFactory = factory;
        PlayerNameInput = string.Empty;
        IsLoginVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmLogin))]
    private void ConfirmLogin()
    {
        if (_pendingGameFactory is null) return;
        var name = PlayerNameInput.Trim();
        var vm = _pendingGameFactory(name);
        IsLoginVisible = false;
        _pendingGameFactory = null;
        GameSelected?.Invoke(vm);
    }

    private bool CanConfirmLogin() => !string.IsNullOrWhiteSpace(PlayerNameInput);

    [RelayCommand]
    private void CancelLogin()
    {
        IsLoginVisible = false;
        _pendingGameFactory = null;
        PlayerNameInput = string.Empty;
    }
}
