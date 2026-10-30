using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

// Runs XexTool (https://github.com/Team-Resurgent/XexTool). It is looked for next to this program, on the PATH, and in
// a per-user folder; if it isn't found, the release for this OS and CPU is downloaded to that folder.
internal static class XexTool
{
    private const string ReleaseUrl = "https://github.com/Team-Resurgent/XexTool/releases/download/latest/";

    private static readonly string FileName = OperatingSystem.IsWindows() ? "XexTool.exe" : "XexTool";

    private static readonly string DownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xbox360Convert", "XexTool");

    public static string? Find()
    {
        var folders = new List<string> { AppContext.BaseDirectory };
        folders.AddRange((Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        folders.Add(DownloadFolder);
        return folders.Select(folder => Path.Combine(folder, FileName)).FirstOrDefault(File.Exists);
    }

    private static string GetAssetName()
    {
        var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        return $"XexTool-{os}-{arch}.zip";
    }

    public static string Download()
    {
        var assetName = GetAssetName();
        Program.LogLine($"Downloading {assetName}...");
        using (var client = new HttpClient())
        using (var zipStream = new MemoryStream(client.GetByteArrayAsync(ReleaseUrl + assetName).GetAwaiter().GetResult()))
        using (var archive = new ZipArchive(zipStream))
        {
            var entry = archive.Entries.FirstOrDefault(e => e.Name == FileName) ?? throw new InvalidDataException($"{assetName} has no {FileName}.");
            Directory.CreateDirectory(DownloadFolder);
            var toolPath = Path.Combine(DownloadFolder, FileName);
            entry.ExtractToFile(toolPath, true);
            if (OperatingSystem.IsWindows() == false)
            {
                File.SetUnixFileMode(toolPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            return toolPath;
        }
    }

    // Returns the converted XEX, or null if XexTool failed.
    public static byte[]? Convert(string toolPath, byte[] xex, string[] options, string workFolder)
    {
        Directory.CreateDirectory(workFolder);
        var inputPath = Path.Combine(workFolder, "input.xex");
        var outputPath = Path.Combine(workFolder, "output.xex");
        File.WriteAllBytes(inputPath, xex);
        File.Delete(outputPath);

        var startInfo = new ProcessStartInfo(toolPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workFolder
        };
        foreach (var option in options)
        {
            startInfo.ArgumentList.Add(option);
        }
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputPath);
        startInfo.ArgumentList.Add(inputPath);

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                return null;
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(stdout, stderr);
        }

        return File.Exists(outputPath) ? File.ReadAllBytes(outputPath) : null;
    }
}
