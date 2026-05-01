using System.Security.Cryptography;

namespace ZaldrionPasswordIntelligence.Services;

public sealed record PasswordGeneratorOptions(
    int Length,
    bool IncludeUppercase,
    bool IncludeLowercase,
    bool IncludeNumbers,
    bool IncludeSymbols,
    bool AvoidAmbiguousCharacters);

public sealed class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Numbers = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?/|~";
    private const string Ambiguous = "0O1Il|`'\"";

    public string Generate(PasswordGeneratorOptions options)
    {
        if (options.Length < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Length), "Use at least 8 characters.");
        }

        List<string> selectedSets = new();

        AddSetIfEnabled(selectedSets, Uppercase, options.IncludeUppercase, options.AvoidAmbiguousCharacters);
        AddSetIfEnabled(selectedSets, Lowercase, options.IncludeLowercase, options.AvoidAmbiguousCharacters);
        AddSetIfEnabled(selectedSets, Numbers, options.IncludeNumbers, options.AvoidAmbiguousCharacters);
        AddSetIfEnabled(selectedSets, Symbols, options.IncludeSymbols, options.AvoidAmbiguousCharacters);

        if (selectedSets.Count == 0)
        {
            throw new InvalidOperationException("Select at least one character group.");
        }

        if (options.Length < selectedSets.Count)
        {
            throw new InvalidOperationException("Length must be equal to or greater than the selected character groups.");
        }

        string combinedSet = string.Concat(selectedSets);
        List<char> passwordCharacters = new(options.Length);

        foreach (string set in selectedSets)
        {
            passwordCharacters.Add(GetRandomCharacter(set));
        }

        while (passwordCharacters.Count < options.Length)
        {
            passwordCharacters.Add(GetRandomCharacter(combinedSet));
        }

        Shuffle(passwordCharacters);
        return new string(passwordCharacters.ToArray());
    }

    private static void AddSetIfEnabled(List<string> selectedSets, string characters, bool isEnabled, bool avoidAmbiguous)
    {
        if (!isEnabled)
        {
            return;
        }

        string safeCharacters = avoidAmbiguous
            ? new string(characters.Where(character => !Ambiguous.Contains(character)).ToArray())
            : characters;

        if (safeCharacters.Length > 0)
        {
            selectedSets.Add(safeCharacters);
        }
    }

    private static char GetRandomCharacter(string characters)
    {
        int index = RandomNumberGenerator.GetInt32(characters.Length);
        return characters[index];
    }

    private static void Shuffle(IList<char> characters)
    {
        for (int i = characters.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            char current = characters[i];
            characters[i] = characters[j];
            characters[j] = current;
        }
    }
}
