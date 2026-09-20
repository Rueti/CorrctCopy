using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;

namespace Correct_Copy;

public partial class MainWindow : Window
{
    private readonly List<CopyOption> options = RobocopyOptions.Create();
    private Process? running;
    private bool cancelled;
    private bool ready;
    private List<string>? arguments;
    private CopyProgress copyProgress = new();

    public MainWindow()
    {
        InitializeComponent();
        options.Single(o => o.Switch == "L").Enabled = true;
        PreviewCheck.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new System.Windows.Data.Binding(nameof(CopyOption.Enabled)) { Source = options.Single(o => o.Switch == "L"), Mode = BindingMode.TwoWay });
        foreach (var option in options) option.PropertyChanged += (_, _) => RefreshCommand();
        ready = true;
        RefreshCommand();
        Closing += OnClosing;
    }

    private void InputChanged(object sender, RoutedEventArgs e) => RefreshCommand();

    private void RefreshCommand()
    {
        if (!ready) return;
        try
        {
            arguments = RobocopyOptions.Build(SourceBox.Text.Trim(), DestinationBox.Text.Trim(), FilterBox.Text.Trim(), options, false);
            CommandBox.Text = "robocopy " + string.Join(" ", arguments.Select(RobocopyOptions.Quote));
            ValidationText.Text = RobocopyOptions.DescribeEffects(options);
            StartButton.IsEnabled = running == null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            arguments = null;
            CommandBox.Text = "";
            ValidationText.Text = ex.Message;
            StartButton.IsEnabled = false;
        }
        StartButton.Content = options.Any(o => o.Enabled && o.Switch == "QUIT") ? "Parameter verarbeiten" :
            options.Single(o => o.Switch == "L").Enabled ? "Testlauf starten" : "Auftrag starten";
    }

    private string? PickFolder(string title)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = title };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }
    private void SourceFolder_Click(object sender, RoutedEventArgs e)
    {
        if (PickFolder("Quellordner auswählen") is { } path) { SourceBox.Text = path; FilterBox.Text = "*.*"; }
    }
    private void Destination_Click(object sender, RoutedEventArgs e)
    {
        if (PickFolder("Zielordner auswählen") is { } path) DestinationBox.Text = path;
    }
    private void SourceFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Quelldatei auswählen", Filter = "Alle Dateien|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            SourceBox.Text = Path.GetDirectoryName(dialog.FileName)!;
            FilterBox.Text = Path.GetFileName(dialog.FileName);
        }
    }
    private void Options_Click(object sender, RoutedEventArgs e) => new Maual_Setting_Window(options) { Owner = this }.ShowDialog();
    private void Preset(params string[] switches)
    {
        foreach (var option in options)
            if (option.Switch != "L") option.Enabled = switches.Contains(option.Switch) || option.Switch is "R" or "W" or "UNICODE";
    }
    private void Simple_Click(object sender, RoutedEventArgs e) => Preset();
    private void Folders_Click(object sender, RoutedEventArgs e) => Preset("E");
    private void Slow_Click(object sender, RoutedEventArgs e) => Preset("Z", "IPG");
    private void CopyCommand_Click(object sender, RoutedEventArgs e)
    {
        if (arguments == null) return;
        try { System.Windows.Clipboard.SetText(CommandBox.Text); StatusText.Text = "Befehl kopiert"; }
        catch (System.Runtime.InteropServices.COMException) { StatusText.Text = "Zwischenablage gerade nicht verfügbar."; }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        RefreshCommand();
        if (arguments == null || running != null) return;
        bool Has(string name) => options.Any(o => o.Enabled && o.Switch == name);
        if (!Has("QUIT") && !Has("NOSD") && SourceBox.Text.Trim().Length > 0 && !Directory.Exists(SourceBox.Text.Trim()))
        { ValidationText.Text = "Der Quellordner existiert nicht oder ist nicht erreichbar."; return; }
        if (Has("JOB"))
        {
            var jobPath = options.Single(o => o.Switch == "JOB").Value.Trim();
            if (!File.Exists(jobPath) && !File.Exists(jobPath + ".rcj"))
            { ValidationText.Text = "Die Auftragsdatei wurde nicht gefunden."; return; }
        }
        bool preview = Has("L"), quit = Has("QUIT");
        bool needsConfirmation = Has("JOB") || Has("REG") || Has("SAVE") || Has("LOG") || Has("UNILOG") ||
            (!preview && !quit && options.Any(o => o.Enabled && o.Switch is "MIR" or "PURGE" or "MOV" or "MOVE"));
        if (needsConfirmation && MessageBox.Show(this, RobocopyOptions.DescribeEffects(options) + "\n\n" + CommandBox.Text + "\n\nAuftrag ausführen?",
                "Auftrag bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "robocopy.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        running = process;
        cancelled = false;
        Configuration.IsEnabled = false; StartButton.IsEnabled = false; CancelButton.IsEnabled = true;
        copyProgress = new CopyProgress();
        RunProgress.Visibility = Visibility.Visible;
        RunProgress.IsIndeterminate = true;
        RunProgress.Value = 0;
        OutputBox.Clear(); StatusText.Text = quit ? "Parameter werden verarbeitet …" : preview ? "Testlauf läuft …" :
            Has("NP") ? "Robocopy läuft – Prozentanzeige durch /NP ausgeschaltet." : "Robocopy läuft – warte auf Dateifortschritt …";
        try
        {
            process.Start();
            await Task.WhenAll(ReadOutput(process.StandardOutput.BaseStream), ReadOutput(process.StandardError.BaseStream), process.WaitForExitAsync());
            StatusText.Text = cancelled ? "Abgebrochen – bereits kopierte Dateien bleiben erhalten." :
                process.ExitCode >= 8 ? $"Fehler (Robocopy-Code {process.ExitCode}). Siehe Ausgabe." :
                process.ExitCode >= 2 ? $"{(preview ? "Testlauf" : "Auftrag")} beendet mit Hinweisen (Code {process.ExitCode}). Siehe Ausgabe." :
                $"{(quit ? "Parameterverarbeitung" : preview ? "Testlauf" : "Auftrag")} erfolgreich (Code {process.ExitCode}).";
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            StatusText.Text = "Auftrag fehlgeschlagen.";
            AppendOutput(ex.Message);
        }
        finally
        {
            running = null; Configuration.IsEnabled = true; CancelButton.IsEnabled = false;
            RunProgress.Visibility = Visibility.Collapsed;
            RefreshCommand();
        }
    }

    private async Task ReadOutput(Stream stream)
    {
        var decoder = new RobocopyOutput(Encoding.GetEncoding((int)GetOEMCP()));
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer.AsMemory())) > 0) AppendOutput(decoder.Decode(buffer.AsSpan(0, count)));
        AppendOutput(decoder.Decode([], true));
    }
    private void AppendOutput(string text)
    {
        if (running != null && copyProgress.Read(text) is double percent)
        {
            // Robocopy percentages refer to the current file, not the entire job.
            RunProgress.IsIndeterminate = false;
            RunProgress.Value = percent;
            StatusText.Text = $"Dateifortschritt: {percent:0.0} % · Robocopy läuft noch.";
        }
        // Bound UI memory even for very large directory trees.
        if (OutputBox.Text.Length > 200_000) OutputBox.Text = OutputBox.Text[^100_000..];
        OutputBox.AppendText(text); OutputBox.ScrollToEnd();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (running is { HasExited: false }) { running.Kill(true); cancelled = true; CancelButton.IsEnabled = false; }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { StatusText.Text = ex.Message; }
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (running == null) return;
        e.Cancel = true;
        StatusText.Text = "Bitte den laufenden Auftrag zuerst abbrechen oder dessen Ende abwarten.";
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetOEMCP();
}
