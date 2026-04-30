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

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Deal() => DealCore();

    protected abstract void DealCore();
}
