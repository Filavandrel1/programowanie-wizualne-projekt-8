using CommunityToolkit.Mvvm.ComponentModel;

namespace projekt8plsdzialaj.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    public MainWindowViewModel()
    {
        _currentView = CreateMenu();
    }

    private MenuViewModel CreateMenu()
    {
        var menu = new MenuViewModel();
        menu.GameSelected += OpenGame;
        return menu;
    }

    private void OpenGame(ViewModelBase gameVm)
    {
        if (gameVm is GameViewModelBase game)
            game.BackRequested += () => CurrentView = CreateMenu();
        CurrentView = gameVm;
    }
}
