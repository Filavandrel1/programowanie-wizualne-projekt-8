using System;
using CommunityToolkit.Mvvm.Input;

namespace projekt8plsdzialaj.ViewModels;

public partial class MenuViewModel : ViewModelBase
{
    public event Action<ViewModelBase>? GameSelected;

    [RelayCommand]
    private void StartWar() => GameSelected?.Invoke(new WarViewModel());

    [RelayCommand]
    private void StartBlackjack() => GameSelected?.Invoke(new BlackjackViewModel());

    [RelayCommand]
    private void StartMakao() => GameSelected?.Invoke(new MakaoViewModel());
}
