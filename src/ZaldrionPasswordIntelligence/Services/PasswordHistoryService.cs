using System.Collections.ObjectModel;

namespace ZaldrionPasswordIntelligence.Services;

public sealed class PasswordHistoryService
{
    private const int MaximumEntries = 25;

    public ObservableCollection<string> Entries { get; } = new();

    public void Add(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        Entries.Insert(0, password);

        while (Entries.Count > MaximumEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }

    public void Clear() => Entries.Clear();
}
