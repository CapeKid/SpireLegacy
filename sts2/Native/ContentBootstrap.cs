using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace SpireLegacy;

public static class ContentBootstrap
{
    public static string? Failure { get; private set; }
    public static bool Ready { get; private set; }
    public static string CacheDirectory => Path.Combine(Runtime.DataDirectory,"cache");
    public static void Prepare()
    {
        Ready = false; Failure = null;
        var log = new StringBuilder("Spire Legacy StS2 content preparation\n");
        try
        {
            Directory.CreateDirectory(Runtime.DataDirectory);
            using var preparationLock = new FileStream(Path.Combine(Runtime.DataDirectory,"content-preparation.lock"),FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,FileShare.None);
            var archive = Path.Combine(Runtime.ModDirectory,"content-reader.zip");
            if (!File.Exists(archive)) throw new FileNotFoundException("The content reader is missing. Install the complete Spire Legacy package.",archive);
            var fingerprint = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive)));
            var runtime = Path.Combine(Runtime.DataDirectory,"reader",fingerprint);
            if (!File.Exists(Path.Combine(runtime,"complete.stamp")))
            {
                Directory.CreateDirectory(runtime);
                ZipFile.ExtractToDirectory(archive,runtime,true);
                File.WriteAllText(Path.Combine(runtime,"complete.stamp"),fingerprint);
            }
            var windows = OperatingSystem.IsWindows();
            if (!windows && !OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Content preparation supports Windows and Linux, including Steam Deck and Proton.");
            var executable = Path.Combine(runtime,windows ? "Reader/ReadRogueLegacy.exe" : "ReaderLinux/bin/python3.12");
            if (!windows) File.SetUnixFileMode(executable,File.GetUnixFileMode(executable) | UnixFileMode.UserExecute);
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = runtime, RedirectStandardOutput = true, RedirectStandardError = true };
            if (!windows) { start.ArgumentList.Add(Path.Combine(runtime,"load_rl.py")); start.Environment["PYTHONPATH"] = Path.Combine(runtime,"ReaderLinux/site"); }
            foreach (var argument in new[] {"--sheet",Path.Combine(runtime,"assets.dat"),"--cache",CacheDirectory,"--no-dialog"}) start.ArgumentList.Add(argument);
            var game = System.Environment.GetEnvironmentVariable("HEIR_RL2_DIR");
            if (!string.IsNullOrWhiteSpace(game)) { start.ArgumentList.Add("--game"); start.ArgumentList.Add(game); }
            log.AppendLine("OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription).AppendLine("Reader: " + executable).AppendLine("RL2 override: " + (game ?? "automatic Steam library discovery"));
            using var child = Process.Start(start) ?? throw new InvalidOperationException("Could not start the owned-content reader.");
            var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(180_000)) { child.Kill(true); throw new TimeoutException("Content preparation exceeded three minutes."); }
            log.AppendLine(output.GetAwaiter().GetResult()).AppendLine(errors.GetAwaiter().GetResult());
            if (child.ExitCode != 0) throw new InvalidDataException("Content reader exited with code " + child.ExitCode + ".");
            foreach (var image in Design.Rows("assets").Select(r=>r.Text("output")).Append("hero.png").Append("source.json"))
                if (!File.Exists(Path.Combine(CacheDirectory,image))) throw new FileNotFoundException("Content preparation did not produce " + image);
            Ready = true; log.AppendLine("Owned Rogue Legacy 2 content is ready for Slay the Spire 2.");
        }
        catch (Exception e) { log.AppendLine(e.ToString()); Failure = log.ToString(); GD.PushError(Failure); }
        finally { var full = log.ToString(); GD.Print(full); File.WriteAllText(Path.Combine(Runtime.DataDirectory,"content-preparation.log"),full); }
    }
}
