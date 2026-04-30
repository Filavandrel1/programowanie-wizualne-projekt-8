using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace projekt8plsdzialaj.Models;

/// <summary>
/// Trwałe składowanie historii rozgrywek (plik JSON w katalogu danych użytkownika).
/// </summary>
public static class GameHistoryStore
{
    private static readonly string FilePath = ResolveFilePath();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly object Lock = new();

    private static string ResolveFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "projekt8plsdzialaj");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "history.json");
    }

    public static List<GameRecord> Load()
    {
        lock (Lock)
        {
            if (!File.Exists(FilePath)) return new List<GameRecord>();
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<GameRecord>>(json) ?? new List<GameRecord>();
            }
            catch
            {
                return new List<GameRecord>();
            }
        }
    }

    public static void Add(GameRecord record)
    {
        lock (Lock)
        {
            var list = Load();
            list.Add(record);
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(list, JsonOptions));
            }
            catch
            {
                // Best-effort – jeśli nie da się zapisać, pomijamy błąd.
            }
        }
    }
}
