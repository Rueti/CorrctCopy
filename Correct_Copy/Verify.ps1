param([string]$AssemblyPath = "$PSScriptRoot\bin\Debug\net8.0-windows7.0\Correct_Copy.dll")
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('CorrectCopy-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$assembly = [System.Security.SecurityElement]::Escape((Resolve-Path -LiteralPath $AssemblyPath).Path)
@"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup>
<ItemGroup><Reference Include="Correct_Copy"><HintPath>$assembly</HintPath></Reference></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $testRoot 'Verify.csproj')
@'
using Correct_Copy;
using System.Diagnostics;
using System.IO;
using System.Text;
static class Verify
{
    static int checks;
    static void Check(bool value, string label) { if (!value) { Console.WriteLine("FAILED: " + label); throw new Exception(label); } checks++; }
    static void Reject(Action action, string label)
    {
        try { action(); } catch (ArgumentException) { checks++; return; }
        throw new Exception("Expected rejection: " + label);
    }
    [STAThread]
    static void Main()
    {
        try { RunAll(); }
        catch (Exception ex) { Console.WriteLine("FAILED after " + checks + " checks: " + ex.GetType().Name + ": " + ex.Message); Environment.ExitCode = 1; }
    }
    static void RunAll()
    {
        var options = RobocopyOptions.Create();
        List<string> Build(string source = @"C:\Source", string dest = @"C:\Target", bool preview = true)
            => RobocopyOptions.Build(source, dest, "*.*", options, preview);
        Check(Build().Contains("/R:3") && Build().Contains("/W:2") && Build().Contains("/L"), "Defaults");
        Check(!Build(preview: false).Contains("/L"), "Real run");
        Check(!Build().Contains("/NP"), "File progress enabled by default");
        var progress = new CopyProgress();
        Check(progress.Read("\r  4") == null && progress.Read("2.5%\r") == 42.5, "Progress split across pipe reads");
        Check(progress.Read("  100%\r  0,5%\r") == 0.5, "Progress resets for next file and accepts decimal comma");
        Check(new CopyProgress().Read("\tNew file 123 report50%.txt\r\n") == null, "Filename is not progress");
        Check(RobocopyOptions.Quote(@"C:\") == "\"C:\\\\\"", "Drive root quoting");
        Reject(() => Build(dest: @"C:\Source"), "Same path");
        Reject(() => Build(dest: @"C:\Source\nested"), "Nested destination");
        Reject(() => Build(source: @"C:\Target\nested"), "Nested source");
        Reject(() => Build(source: "relative"), "Relative source");
        Reject(() => Build(source: @"C:\", dest: @"C:\Target"), "Nested destination under drive root");
        CopyOption Get(string name) => options.Single(o => o.Switch == name);
        Get("MT").Enabled = true; Get("IPG").Enabled = true;
        Reject(() => Build(), "MT/IPG conflict"); Get("IPG").Enabled = false;
        Get("MT").Value = "129"; Reject(() => Build(), "MT upper limit");
        Get("MT").Value = "0"; Reject(() => Build(), "MT lower limit");
        Get("MT").Value = "128"; Check(Build().Contains("/MT:128"), "MT boundary"); Get("MT").Enabled = false;
        Get("COPY").Enabled = true; Get("COPY").Value = "BAD";
        Reject(() => Build(), "Invalid flags"); Get("COPY").Value = "dat";
        Check(Build().Contains("/COPY:DAT"), "Normalize flags");
        Get("COPY").Enabled = false; Get("SECFIX").Enabled = true;
        Reject(() => Build(), "SECFIX requires security flags"); Get("SECFIX").Enabled = false;
        Get("S").Enabled = true; Get("E").Enabled = true;
        Reject(() => Build(), "Recursion conflict"); Get("S").Enabled = false; Get("E").Enabled = false;
        var expected = "S E LEV Z B ZB J EFSRAW COPY SEC COPYALL NOCOPY SECFIX TIMFIX PURGE MIR MOV MOVE A+ A- CREATE FAT 256 MON MOT RH PF IPG SJ SL MT DCOPY NODCOPY NOOFFLOAD COMPRESS A M IA XA XF XD XC XN XO XX XL IS IT MAX MIN MAXAGE MINAGE MAXLAD MINLAD FFT DST XJ XJD XJF IM R W REG TBD LFSM L X V TS FP BYTES NS NC NFL NDL NP ETA LOG LOG+ UNILOG UNILOG+ TEE NJH NJS UNICODE JOB SAVE QUIT NOSD NODD IF".Split(' ');
        Check(expected.ToHashSet().SetEquals(options.Select(o => o.Switch)), "Complete supplied help catalog");
        Check(options.Count == expected.Length, "No duplicate switches");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var oem = Encoding.GetEncoding(850);
        var mixed = oem.GetBytes("Kopf: ä\r\nOptionen: ").Concat(Encoding.Unicode.GetBytes("/L /UNICODE\r\n\tGrüße 文件.txt\r\n")).Concat(oem.GetBytes("Ende\r\n")).ToArray();
        foreach (int chunkSize in new[] { 1, 2, 3, 7, 4096 })
        {
            var decoder = new RobocopyOutput(oem); var decoded = new StringBuilder();
            for (int offset = 0; offset < mixed.Length; offset += chunkSize)
                decoded.Append(decoder.Decode(mixed.AsSpan(offset, Math.Min(chunkSize, mixed.Length - offset))));
            decoded.Append(decoder.Decode([], true));
            Check(decoded.ToString() == "Kopf: ä\r\nOptionen: /L /UNICODE\r\n\tGrüße 文件.txt\r\nEnde\r\n", "Mixed encoding with chunk size " + chunkSize);
        }
        var original = options.ToDictionary(o => o.Switch, o => (o.Enabled, o.Value));
        void Reset()
        {
            foreach (var o in options) { o.Enabled = original[o.Switch].Enabled; o.Value = original[o.Switch].Value; }
        }
        foreach (var conflict in new[] { "MT", "EFSRAW", "B", "ZB" })
        {
            Reset(); Get("LFSM").Enabled = true; Get(conflict).Enabled = true;
            Reject(() => Build(), "LFSM conflict " + conflict);
        }
        Reset(); Get("LFSM").Enabled = true;
        Check(Build().Contains("/LFSM"), "LFSM optional floor");
        Get("LFSM").Value = "10g"; Check(Build().Contains("/LFSM:10G"), "LFSM size suffix");
        Get("LFSM").Value = "99999999999999999G"; Reject(() => Build(), "LFSM overflow");
        Reset(); Get("MT").Enabled = true; Get("MT").Value = "";
        Check(Build().Contains("/MT"), "MT optional count");
        Reset(); Get("MAX").Enabled = true; Get("MAX").Value = "5368709120";
        Check(Build().Contains("/MAX:5368709120"), "64-bit file size");
        Get("MIN").Enabled = true; Get("MIN").Value = "5368709121";
        Reject(() => Build(), "Inverted size bounds");
        Reset(); Get("MAXAGE").Enabled = true; Get("MAXAGE").Value = "20260230";
        Reject(() => Build(), "Impossible date");
        Get("MAXAGE").Value = "20240229"; Check(Build().Contains("/MAXAGE:20240229"), "Leap date");
        Get("MAXAGE").Value = "1900"; Reject(() => Build(), "Invalid age/date boundary");
        Reset(); Get("RH").Enabled = true; Get("RH").Value = "2200-0600";
        Check(Build().Contains("/RH:2200-0600"), "Overnight schedule");
        Get("RH").Value = "2500-0600"; Reject(() => Build(), "Invalid hour");
        Reset(); Get("XF").Enabled = true; Get("XF").Value = @"*.tmp; C:\My Folder\Keep.txt";
        var exclusions = Build(); var ix = exclusions.IndexOf("/XF");
        Check(exclusions[ix + 1] == "*.tmp" && exclusions[ix + 2] == @"C:\My Folder\Keep.txt", "Exclude list and spaces preserved");
        Get("XF").Value = "*.tmp; /MIR"; Reject(() => Build(), "Exclude list switch injection");
        Reset(); Get("LOG").Enabled = true; Get("LOG").Value = @"C:\My Logs\Run.txt";
        Check(Build().Contains(@"/LOG:C:\My Logs\Run.txt"), "Log path case and spaces preserved");
        Get("LOG+").Enabled = true; Get("LOG+").Value = @"C:\My Logs\Run.txt";
        Reject(() => Build(), "Conflicting logging modes");
        Reset(); Get("NOSD").Enabled = true; Get("NODD").Enabled = true;
        Reject(() => Build(), "Omitted directories require job or quit");
        Get("QUIT").Enabled = true;
        Check(Build("", "").Contains("/NOSD") && Build("", "").Contains("/NODD"), "Job template without directories");
        Reset(); Get("L").Enabled = true;
        Check(Build(preview: false).Count(a => a == "/L") == 1 && Build().Count(a => a == "/L") == 1, "L checkbox and dry run deduplicated");
        Reset(); Get("NP").Enabled = false; Get("UNICODE").Enabled = false;
        Check(!Build().Contains("/NP") && !Build().Contains("/UNICODE"), "Output flags selectable");
        Reset();
        Check(RobocopyOptions.Build(@"C:\Source", @"C:\Target", "*.pdf;My Document.txt", options, true).Skip(1).Take(4).SequenceEqual(new[] { @"C:\Source", @"C:\Target", "*.pdf", "My Document.txt" }), "Multiple source patterns");
        var app = new System.Windows.Application();
        var window = new MainWindow();
        Check(window.FindName("StartButton") is System.Windows.Controls.Button { IsEnabled: false }, "Initial UI validation");
        var settings = new Maual_Setting_Window(options);
        var optionsContent = (System.Windows.FrameworkElement)settings.Content;
        optionsContent.Measure(new System.Windows.Size(1000, 600));
        optionsContent.Arrange(new System.Windows.Rect(0, 0, 1000, 600));
        optionsContent.UpdateLayout();
        Check(optionsContent.ActualWidth > 0, "Options templates rendered");
        Check(settings.FindName("OptionsList") != null, "Settings XAML loads");
        ((System.Windows.Controls.TextBox)settings.FindName("SearchBox")).Text = "/LFSM";
        Check(((System.Windows.Controls.ItemsControl)settings.FindName("OptionsList")).Items.Count == 1, "Parameter search");
        ((System.Windows.Controls.TextBox)window.FindName("SourceBox")).Text = @"C:\Source";
        ((System.Windows.Controls.TextBox)window.FindName("DestinationBox")).Text = @"C:\Target";
        var previewControl = (System.Windows.Controls.CheckBox)window.FindName("PreviewCheck");
        previewControl.IsChecked = false;
        Check(!((System.Windows.Controls.TextBox)window.FindName("CommandBox")).Text.Contains("\"/L\""), "Main preview checkbox updates command");
        // Exercise the actual button handler and nested modal dispatcher, not just construction.
        app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        window.ShowActivated = false; window.ShowInTaskbar = false;
        window.Show(); window.Hide();
        bool modalOpened = false;
        window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
        {
            var modal = app.Windows.OfType<Maual_Setting_Window>().Single(w => w.IsVisible);
            modal.UpdateLayout();
            modalOpened = ((System.Windows.Controls.ItemsControl)modal.FindName("OptionsList")).Items.Count == 91;
            modal.Close();
        }));
        IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject root)
        {
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(root).OfType<System.Windows.DependencyObject>())
            { yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
        }
        var optionsButton = Descendants(window).OfType<System.Windows.Controls.Button>().Single(b => b.Content as string == "Optionen auswählen …");
        optionsButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Check(modalOpened, "Options button opens and closes modal with 91 rendered options");
        settings.Close(); window.Close();
        var root = Path.Combine(Path.GetTempPath(), "CorrectCopy-data-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "Source with spaces"); var target = Path.Combine(root, "Target with spaces");
        Directory.CreateDirectory(source); Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(source, "Grüße & Test.txt"), "Test content");
        File.WriteAllText(Path.Combine(target, "Keep.txt"), "Keep");
        int Run(bool preview, bool rendered = false)
        {
            var args = Get("JOB").Enabled ? Build("", "", preview) : Build(source, target, preview);
            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "robocopy.exe"))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.Unicode };
            if (rendered) start.Arguments = string.Join(" ", args.Select(RobocopyOptions.Quote));
            else foreach (var arg in args) start.ArgumentList.Add(arg);
            using var proc = Process.Start(start)!;
            using var raw = new MemoryStream();
            proc.StandardOutput.BaseStream.CopyTo(raw); proc.WaitForExit();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var output = new RobocopyOutput(Encoding.GetEncoding(850)).Decode(raw.ToArray(), true);
            Check(proc.ExitCode < 8, "Robocopy exit: " + output);
            Check(output.Contains("ROBOCOPY") || Get("QUIT").Enabled, "Readable Unicode output: " + output[..Math.Min(300, output.Length)]);
            return proc.ExitCode;
        }
        Get("MIR").Enabled = true;
        Run(true, true);
        Check(!File.Exists(Path.Combine(target, "Grüße & Test.txt")) && File.Exists(Path.Combine(target, "Keep.txt")), "MIR dry run changes nothing");
        Get("MIR").Enabled = false;
        Run(false);
        Check(File.ReadAllText(Path.Combine(target, "Grüße & Test.txt")) == "Test content", "Copy Unicode filename");
        Check(File.Exists(Path.Combine(target, "Keep.txt")), "Ordinary copy preserves extra files");
        Check(Run(false) < 8, "Repeated copy is successful");
        File.WriteAllText(Path.Combine(source, "Ignored.tmp"), "Skip");
        Get("XF").Enabled = true; Get("XF").Value = "*.tmp";
        Get("UNILOG").Enabled = true; Get("UNILOG").Value = Path.Combine(root, "Unicode Log.txt");
        Get("TEE").Enabled = true;
        Run(false, true);
        Check(!File.Exists(Path.Combine(target, "Ignored.tmp")), "Live exclusion");
        Check(File.Exists(Get("UNILOG").Value) && File.ReadAllText(Get("UNILOG").Value, Encoding.Unicode).Length > 0, "Unicode log with spaces");
        Reset();
        Get("SAVE").Enabled = true; Get("SAVE").Value = Path.Combine(root, "Saved Job.rcj");
        Get("QUIT").Enabled = true;
        Run(false, true);
        Check(File.Exists(Get("SAVE").Value), "Save job with QUIT");
        Check(!File.Exists(Path.Combine(target, "Ignored.tmp")), "QUIT does not copy");
        Get("JOB").Enabled = true; Get("JOB").Value = Get("SAVE").Value; Get("SAVE").Enabled = false; Get("QUIT").Enabled = false;
        Run(true, true);
        Check(!File.Exists(Path.Combine(target, "Ignored.tmp")), "Loaded job respects explicit dry run");
        Reset(); Get("SAVE").Enabled = true; Get("SAVE").Value = Path.Combine(root, "Template.rcj");
        Get("QUIT").Enabled = true; Get("NOSD").Enabled = true; Get("NODD").Enabled = true;
        Run(false, true);
        Check(File.Exists(Get("SAVE").Value), "Save template without source or destination");
        var uiSource = Path.Combine(root, "UI source"); var uiTarget = Path.Combine(root, "UI target");
        Directory.CreateDirectory(uiSource); Directory.CreateDirectory(uiTarget);
        File.WriteAllBytes(Path.Combine(uiSource, "Payload.bin"), new byte[4 * 1024 * 1024]);
        var copyUi = new MainWindow { ShowActivated = false, ShowInTaskbar = false };
        copyUi.Show(); copyUi.Hide();
        ((System.Windows.Controls.TextBox)copyUi.FindName("SourceBox")).Text = uiSource;
        ((System.Windows.Controls.TextBox)copyUi.FindName("DestinationBox")).Text = uiTarget;
        ((System.Windows.Controls.CheckBox)copyUi.FindName("PreviewCheck")).IsChecked = false;
        var startButton = (System.Windows.Controls.Button)copyUi.FindName("StartButton");
        copyUi.Dispatcher.BeginInvoke(new Action(() => startButton.RaiseEvent(
            new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent))));
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timeout = Stopwatch.StartNew();
        var poll = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        poll.Tick += (_, _) => { if (startButton.IsEnabled || timeout.Elapsed.TotalSeconds > 15) frame.Continue = false; };
        poll.Start(); System.Windows.Threading.Dispatcher.PushFrame(frame); poll.Stop();
        Check(startButton.IsEnabled, "Real GUI copy releases controls after process exit");
        Check(((System.Windows.Controls.TextBlock)copyUi.FindName("StatusText")).Text.Contains("erfolgreich"), "Real GUI copy reports completion");
        Check(((System.Windows.Controls.ProgressBar)copyUi.FindName("RunProgress")).Visibility == System.Windows.Visibility.Collapsed, "Busy indicator stops after completion");
        Check(new FileInfo(Path.Combine(uiTarget, "Payload.bin")).Length == 4 * 1024 * 1024, "GUI copy writes complete file");
        copyUi.Close();
        Console.WriteLine($"PASS: {checks} checks. Temporary test files: {root}");
    }
}
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Program.cs') -Encoding utf8
dotnet restore (Join-Path $testRoot 'Verify.csproj') --source (Join-Path $env:USERPROFILE '.nuget\packages')
if ($LASTEXITCODE -ne 0) { throw 'Verification restore failed.' }
dotnet run --no-restore --project (Join-Path $testRoot 'Verify.csproj')
if ($LASTEXITCODE -ne 0) { throw 'Verification failed.' }
