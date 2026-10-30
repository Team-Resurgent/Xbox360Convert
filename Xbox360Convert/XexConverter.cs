using System.Buffers.Binary;
using System.Text;
using XboxToolkit;
using XboxToolkit.Models;

// Converts every XEX in an extracted disc with XexTool, including the XEX files inside STFS packages, which are rebuilt
// around the converted files.
internal static class XexConverter
{
    private static bool HasMagic(byte[] data, string magic)
    {
        return data.Length >= 4 && Encoding.ASCII.GetString(data, 0, 4) == magic;
    }

    // Title update deltas (.xexp) only apply to the original XEX, so they are left as they are.
    private static bool IsConvertibleXex(byte[] data)
    {
        return HasMagic(data, "XEX2") && data.Length >= 8 && (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4)) & 0x30) == 0;
    }

    private static byte[] ReadHeader(string path)
    {
        using (var stream = File.OpenRead(path))
        {
            var header = new byte[Math.Min(stream.Length, 0x1000)];
            stream.ReadExactly(header);
            return header;
        }
    }

    // Returns the converted XEX, or null if it is unchanged or couldn't be converted.
    private static byte[]? ConvertXex(string toolPath, string[] options, string workFolder, string path, byte[] xex)
    {
        var converted = XexTool.Convert(toolPath, xex, options, workFolder);
        if (converted == null)
        {
            Program.LogLine($"Warning: XexTool failed on {path}, left unchanged.");
            return null;
        }
        if (converted.AsSpan().SequenceEqual(xex))
        {
            return null;
        }
        Program.LogLine($"  {path}");
        return converted;
    }

    // A rebuilt package no longer matches its signature. It is written as a console-signed (CON) package with a blank
    // certificate and signature, which 360Attach fills in at mount time with the console's own key.
    private static byte[] ToUnsignedCon(byte[] package)
    {
        Encoding.ASCII.GetBytes("CON ").CopyTo(package, 0);
        Array.Clear(package, 4, 0x228);
        return package;
    }

    public static void ConvertFolder(string folder, string toolPath, string[] options, string workFolder)
    {
        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.GetRelativePath(folder, file);
            // The system update and title update on the disc are left alone.
            if (path.StartsWith("$SystemUpdate", StringComparison.OrdinalIgnoreCase) || path.StartsWith("$TitleUpdate", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var header = ReadHeader(file);
            if (IsConvertibleXex(header))
            {
                var converted = ConvertXex(toolPath, options, workFolder, path, File.ReadAllBytes(file));
                if (converted != null)
                {
                    File.WriteAllBytes(file, converted);
                }
            }
            else if (StfsPackage.IsStfs(header))
            {
                var package = new StfsPackage(File.ReadAllBytes(file));
                var replacements = new Dictionary<StfsFileEntry, byte[]>();
                foreach (var entry in package.Entries)
                {
                    if (entry.IsDirectory)
                    {
                        continue;
                    }
                    var entryData = package.ReadFile(entry);
                    if (IsConvertibleXex(entryData))
                    {
                        var converted = ConvertXex(toolPath, options, workFolder, $"{path} : {entry.Path}", entryData);
                        if (converted != null)
                        {
                            replacements[entry] = converted;
                        }
                    }
                }
                if (replacements.Count > 0)
                {
                    File.WriteAllBytes(file, ToUnsignedCon(package.Rebuild(replacements)));
                }
            }
        }
    }
}
