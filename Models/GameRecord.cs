using System;

namespace projekt8plsdzialaj.Models;

/// <summary>
/// Pojedynczy wpis w historii rozgrywek.
/// </summary>
public sealed class GameRecord
{
    public string PlayerName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; } = DateTime.Now;

    public string PlayedAtDisplay => PlayedAt.ToString("yyyy-MM-dd HH:mm");
}
