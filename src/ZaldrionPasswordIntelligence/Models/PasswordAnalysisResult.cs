namespace ZaldrionPasswordIntelligence.Models;

public sealed class PasswordAnalysisResult
{
    public string Password { get; init; } = string.Empty;
    public int Length { get; init; }
    public int Score { get; init; }
    public double EntropyBits { get; init; }
    public int CharacterPoolSize { get; init; }
    public PasswordStrengthLevel Level { get; init; }
    public string LevelLabel { get; init; } = "Sin analizar";
    public string EstimatedCrackTime { get; init; } = "—";
    public string CharacterSetSummary { get; init; } = "—";
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
}
