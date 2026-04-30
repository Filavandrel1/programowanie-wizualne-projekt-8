using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using projekt8plsdzialaj.Models;

namespace projekt8plsdzialaj.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    public event Action? BackRequested;

    public ObservableCollection<GameRecord> Records { get; } = new();

    public bool IsEmpty => Records.Count == 0;

    public HistoryViewModel()
    {
        Reload();
    }

    private void Reload()
    {
        Records.Clear();
        // Najnowsze na górze.
        foreach (var r in GameHistoryStore.Load().OrderByDescending(r => r.PlayedAt))
            Records.Add(r);
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    [RelayCommand]
    private void Refresh() => Reload();
}
