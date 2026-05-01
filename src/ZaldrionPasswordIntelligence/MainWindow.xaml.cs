using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using ZaldrionPasswordIntelligence.Models;
using ZaldrionPasswordIntelligence.Services;

namespace ZaldrionPasswordIntelligence;

public partial class MainWindow : Window
{
    private readonly PasswordGenerator _generator = new();
    private readonly PasswordAnalyzer _analyzer = new();
    private readonly PasswordHistoryService _history = new();

    public MainWindow()
    {
        InitializeComponent();
        HistoryList.ItemsSource = _history.Entries;
        UpdateLengthText();
        GeneratePassword();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e) => GeneratePassword();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyGeneratedPassword(showMessage: true);
    }

    private void AnalyzeManualButton_Click(object sender, RoutedEventArgs e)
    {
        string password = ManualPasswordBox.Password;
        if (string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Escribe o pega una contraseña para analizarla.", "Análisis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        UpdateAnalysis(_analyzer.Analyze(password));
    }

    private void ManualPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ManualPasswordBox.Password))
        {
            UpdateAnalysis(_analyzer.Analyze(ManualPasswordBox.Password));
        }
    }

    private void LengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateLengthText();
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_history.Entries.Count == 0)
        {
            MessageBox.Show("No hay contraseñas en el historial de esta sesión.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            "Vas a exportar contraseñas en texto plano. Hazlo solo si es para pruebas o si guardarás el archivo en un lugar seguro.",
            "Confirmar exportación",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Exportar historial de contraseñas",
            Filter = "Archivo de texto (*.txt)|*.txt",
            FileName = $"zaldrion-passwords-{DateTime.Now:yyyyMMdd-HHmm}.txt"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllLines(dialog.FileName, _history.Entries);
        MessageBox.Show("Archivo exportado correctamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void GeneratePassword()
    {
        try
        {
            PasswordGeneratorOptions options = new(
                Length: (int)Math.Round(LengthSlider.Value),
                IncludeUppercase: UppercaseCheckBox.IsChecked == true,
                IncludeLowercase: LowercaseCheckBox.IsChecked == true,
                IncludeNumbers: NumbersCheckBox.IsChecked == true,
                IncludeSymbols: SymbolsCheckBox.IsChecked == true,
                AvoidAmbiguousCharacters: AvoidAmbiguousCheckBox.IsChecked == true);

            string password = _generator.Generate(options);
            GeneratedPasswordTextBox.Text = password;
            _history.Add(password);
            UpdateAnalysis(_analyzer.Analyze(password));

            if (AutoCopyCheckBox.IsChecked == true)
            {
                CopyGeneratedPassword(showMessage: false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo generar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyGeneratedPassword(bool showMessage)
    {
        string password = GeneratedPasswordTextBox.Text;
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        Clipboard.SetText(password);

        if (showMessage)
        {
            MessageBox.Show("Contraseña copiada al portapapeles.", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdateAnalysis(PasswordAnalysisResult result)
    {
        ScoreBar.Value = result.Score;
        ScoreBar.Foreground = GetScoreBrush(result.Score);
        ScoreText.Text = $"{result.Score}/100";
        StrengthLabelText.Text = $"Fuerza: {result.LevelLabel}";
        EntropyText.Text = $"{result.EntropyBits:0.0} bits";
        CrackTimeText.Text = result.EstimatedCrackTime;
        CharacterSetText.Text = $"Caracteres detectados: {result.CharacterSetSummary} · pool estimado: {result.CharacterPoolSize}";
        FindingsList.ItemsSource = result.Findings;
    }

    private void UpdateLengthText()
    {
        if (LengthValueText is null || LengthSlider is null)
        {
            return;
        }

        LengthValueText.Text = $"{(int)Math.Round(LengthSlider.Value)} caracteres";
    }

    private Brush GetScoreBrush(int score)
    {
        string resourceKey = score switch
        {
            >= 80 => "SuccessBrush",
            >= 55 => "WarningBrush",
            _ => "DangerBrush"
        };

        return TryFindResource(resourceKey) as Brush ?? Brushes.MediumPurple;
    }
}
