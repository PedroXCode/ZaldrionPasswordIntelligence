using System.Text.RegularExpressions;
using ZaldrionPasswordIntelligence.Models;

namespace ZaldrionPasswordIntelligence.Services;

public sealed class PasswordAnalyzer
{
    private const double OfflineGuessesPerSecond = 1_000_000_000_000D;

    private static readonly string[] CommonTerms =
    {
        "password", "pass", "admin", "administrator", "welcome", "login", "qwerty", "letmein",
        "dragon", "football", "baseball", "soccer", "iloveyou", "monkey", "abc", "abcd",
        "1234", "12345", "123456", "0000", "1111", "2024", "2025", "2026"
    };

    private static readonly string[] KeyboardSequences =
    {
        "qwertyuiop", "asdfghjkl", "zxcvbnm", "1234567890", "0987654321",
        "poiuytrewq", "lkjhgfdsa", "mnbvcxz"
    };

    public PasswordAnalysisResult Analyze(string? password)
    {
        password ??= string.Empty;
        List<string> findings = new();

        if (password.Length == 0)
        {
            return new PasswordAnalysisResult
            {
                Password = password,
                Length = 0,
                Score = 0,
                Level = PasswordStrengthLevel.Empty,
                LevelLabel = "Sin analizar",
                Findings = new[] { "Escribe o genera una contraseña para analizarla." }
            };
        }

        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(character => !char.IsLetterOrDigit(character));

        int characterPoolSize = 0;
        List<string> usedGroups = new();

        if (hasLower)
        {
            characterPoolSize += 26;
            usedGroups.Add("minúsculas");
        }

        if (hasUpper)
        {
            characterPoolSize += 26;
            usedGroups.Add("mayúsculas");
        }

        if (hasDigit)
        {
            characterPoolSize += 10;
            usedGroups.Add("números");
        }

        if (hasSymbol)
        {
            characterPoolSize += 32;
            usedGroups.Add("símbolos");
        }

        double rawEntropy = characterPoolSize == 0 ? 0 : password.Length * Math.Log2(characterPoolSize);
        double adjustedEntropy = rawEntropy;
        int penalty = 0;

        if (password.Length < 10)
        {
            penalty += 28;
            findings.Add("Muy corta: usa al menos 12 caracteres, idealmente 16 o más.");
        }
        else if (password.Length < 14)
        {
            penalty += 12;
            findings.Add("Aceptable, pero sería mejor subirla a 16+ caracteres.");
        }
        else
        {
            findings.Add("Buen largo para una contraseña moderna.");
        }

        int groupCount = new[] { hasLower, hasUpper, hasDigit, hasSymbol }.Count(value => value);
        if (groupCount < 3)
        {
            penalty += 20;
            findings.Add("Poca variedad: mezcla letras, números y símbolos.");
        }
        else if (groupCount == 4)
        {
            findings.Add("Buena variedad de caracteres.");
        }

        if (ContainsCommonTerm(password))
        {
            adjustedEntropy *= 0.55;
            penalty += 30;
            findings.Add("Contiene una palabra o secuencia común fácil de adivinar.");
        }

        if (ContainsKeyboardSequence(password))
        {
            adjustedEntropy *= 0.65;
            penalty += 22;
            findings.Add("Tiene secuencias de teclado, como qwerty o 123456.");
        }

        if (HasLongRepeatedCharacters(password))
        {
            adjustedEntropy *= 0.72;
            penalty += 18;
            findings.Add("Tiene caracteres repetidos varias veces seguidas.");
        }

        if (HasRepeatedPattern(password))
        {
            adjustedEntropy *= 0.68;
            penalty += 18;
            findings.Add("Parece usar un patrón repetido.");
        }

        if (IsOnlyLettersOrOnlyNumbers(password))
        {
            adjustedEntropy *= 0.62;
            penalty += 25;
            findings.Add("Solo usa letras o solo números; eso reduce bastante la seguridad.");
        }

        adjustedEntropy = Math.Max(0, adjustedEntropy);
        int score = CalculateScore(adjustedEntropy, penalty);
        PasswordStrengthLevel level = GetLevel(score);

        if (score >= 85)
        {
            findings.Add("Excelente para uso general si no la reutilizas en otras cuentas.");
        }
        else if (score >= 65)
        {
            findings.Add("Buena, pero todavía puedes mejorarla aumentando el largo.");
        }

        return new PasswordAnalysisResult
        {
            Password = password,
            Length = password.Length,
            Score = score,
            EntropyBits = adjustedEntropy,
            CharacterPoolSize = characterPoolSize,
            Level = level,
            LevelLabel = GetLevelLabel(level),
            EstimatedCrackTime = EstimateCrackTime(adjustedEntropy),
            CharacterSetSummary = usedGroups.Count == 0 ? "—" : string.Join(", ", usedGroups),
            Findings = findings.Distinct().ToArray()
        };
    }

    private static int CalculateScore(double entropyBits, int penalty)
    {
        int entropyScore = (int)Math.Round(Math.Min(100, entropyBits * 1.18));
        return Math.Clamp(entropyScore - penalty, 0, 100);
    }

    private static PasswordStrengthLevel GetLevel(int score) => score switch
    {
        >= 90 => PasswordStrengthLevel.Excellent,
        >= 75 => PasswordStrengthLevel.Strong,
        >= 55 => PasswordStrengthLevel.Fair,
        >= 30 => PasswordStrengthLevel.Weak,
        _ => PasswordStrengthLevel.VeryWeak
    };

    private static string GetLevelLabel(PasswordStrengthLevel level) => level switch
    {
        PasswordStrengthLevel.Excellent => "Excelente",
        PasswordStrengthLevel.Strong => "Fuerte",
        PasswordStrengthLevel.Fair => "Media",
        PasswordStrengthLevel.Weak => "Débil",
        PasswordStrengthLevel.VeryWeak => "Muy débil",
        _ => "Sin analizar"
    };

    private static bool ContainsCommonTerm(string password)
    {
        string normalized = password.ToLowerInvariant();
        return CommonTerms.Any(normalized.Contains);
    }

    private static bool ContainsKeyboardSequence(string password)
    {
        string normalized = password.ToLowerInvariant();
        return KeyboardSequences.Any(sequence => ContainsSequence(normalized, sequence, 4));
    }

    private static bool ContainsSequence(string password, string sequence, int minimumLength)
    {
        for (int length = minimumLength; length <= sequence.Length; length++)
        {
            for (int start = 0; start <= sequence.Length - length; start++)
            {
                if (password.Contains(sequence.Substring(start, length), StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasLongRepeatedCharacters(string password) => Regex.IsMatch(password, @"(.)\1{2,}");

    private static bool HasRepeatedPattern(string password)
    {
        if (password.Length < 6)
        {
            return false;
        }

        return Regex.IsMatch(password, @"^(.{2,})\1+$");
    }

    private static bool IsOnlyLettersOrOnlyNumbers(string password)
    {
        return password.All(char.IsLetter) || password.All(char.IsDigit);
    }

    private static string EstimateCrackTime(double entropyBits)
    {
        if (entropyBits <= 0)
        {
            return "instantáneo";
        }

        double logSeconds = ((entropyBits - 1) * Math.Log(2)) - Math.Log(OfflineGuessesPerSecond);
        if (double.IsNaN(logSeconds) || logSeconds < Math.Log(1))
        {
            return "menos de 1 segundo";
        }

        double seconds = Math.Exp(Math.Min(logSeconds, Math.Log(double.MaxValue)));

        if (seconds < 60) return $"{seconds:0} segundos";
        if (seconds < 3600) return $"{seconds / 60:0.0} minutos";
        if (seconds < 86400) return $"{seconds / 3600:0.0} horas";
        if (seconds < 31_536_000) return $"{seconds / 86400:0.0} días";

        double years = seconds / 31_536_000;
        if (years < 1_000) return $"{years:0.0} años";
        if (years < 1_000_000) return $"{years / 1_000:0.0} mil años";
        if (years < 1_000_000_000) return $"{years / 1_000_000:0.0} millones de años";
        return "miles de millones de años";
    }
}
