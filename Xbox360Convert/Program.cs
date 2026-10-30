using Mono.Options;
using SharpCompress.Archives;
using XboxToolkit;
using XboxToolkit.Interface;

internal class Program
{
    private static bool shouldShowHelp = false;
    private static bool delete = false;
    private static bool noSplit = false;
    private static string input = string.Empty;
    private static string output = string.Empty;
    private static string format = "CCI";
    private static string machine = string.Empty;
    private static bool unrestrict = false;
    private static string xexToolPath = string.Empty;

    // Just under the FATX 4GB file size limit.
    private const long FatxSplitPoint = 0xFF000000L;

    // XexTool's "-r a" without "z", so title updates still match the media ID.
    private const string RemoveLimits = "mrbdiyvklc";

    public static string[] GetXexOptions()
    {
        var options = new List<string>();
        if (machine.Length > 0)
        {
            options.Add("-m");
            options.Add(machine.Equals("DEVKIT", StringComparison.OrdinalIgnoreCase) ? "d" : "r");
        }
        if (unrestrict)
        {
            options.Add("-r");
            options.Add(RemoveLimits);
        }
        return options.ToArray();
    }

    public static void LogLine(string line)
    {
        Console.WriteLine(line);
    }

    public static string? Unpack(string inputFile)
    {
        LogLine("Extracting...");

        var tempFolder = Path.Combine(output, "Temp");
        using (var archiveStream = File.OpenRead(inputFile))
        using (var archive = ArchiveFactory.OpenArchive(archiveStream))
        {
            foreach (var entry in archive.Entries)
            {
                if (!Path.GetExtension(entry.Key).Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                entry.WriteToDirectory(tempFolder);
                return Path.Combine(tempFolder, entry.Key);
            }
        }

        return null;
    }

    // Split images are named name.1.ext, name.2.ext, ... Returns the part number, or 0 if the file isn't part of a split set.
    public static int GetSplitPart(string path)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        var subExtension = Path.GetExtension(nameWithoutExtension);
        if (subExtension.Length != 2 || char.IsDigit(subExtension[1]) == false)
        {
            return 0;
        }
        var basePath = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(nameWithoutExtension));
        var extension = Path.GetExtension(path);
        return File.Exists(basePath + ".1" + extension) && File.Exists(basePath + ".2" + extension) ? subExtension[1] - '0' : 0;
    }

    // A GOD container is a header file named by hash next to a hash.data folder, usually under TitleID\00007000\.
    public static string[] GetGODHeaders(string folder)
    {
        return Directory.GetDirectories(folder, "*.data", SearchOption.AllDirectories).Select(s => s[..^".data".Length]).Where(GODContainerReader.IsGOD).ToArray();
    }

    // Names GOD output after the title name in its header, adding the disc number for multi-disc titles.
    public static string GetGODName(string headerPath)
    {
        var buffer = new byte[0x80];
        string name;
        byte discNumber, discsInSet;
        using (var stream = File.OpenRead(headerPath))
        {
            stream.Position = 0x360;
            var titleId = new byte[4];
            stream.ReadExactly(titleId);
            stream.Position = 0x366;
            discNumber = (byte)stream.ReadByte();
            discsInSet = (byte)stream.ReadByte();
            stream.Position = 0x411;
            stream.ReadExactly(buffer);
            name = System.Text.Encoding.BigEndianUnicode.GetString(buffer).TrimEnd('\0').Trim();
            if (name.Length == 0)
            {
                name = Convert.ToHexString(titleId);
            }
        }

        foreach (var c in "\\/:*?\"<>|")
        {
            name = name.Replace(c, '_');
        }
        // FATX file names are ASCII only.
        name = new string(name.Where(c => c < 0x80 && char.IsControl(c) == false).ToArray()).Trim();
        return discsInSet > 1 ? $"{name} (Disc {discNumber})" : name;
    }

    public static bool IsXbox360(ContainerReader container)
    {
        return container.TryGetDefault(out _, out var containerType) && containerType == ContainerType.Xbox360;
    }

    public static bool IsImageFile(string path)
    {
        return path.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) || path.EndsWith(".iso", StringComparison.CurrentCultureIgnoreCase) || path.EndsWith(".cci", StringComparison.CurrentCultureIgnoreCase);
    }

    public static string[] GetInputFiles()
    {
        if (Directory.Exists(input))
        {
            // The container readers open every part of a split set from part 1, so skip the other parts.
            return Directory.GetFiles(input).Where(IsImageFile).Where(s => GetSplitPart(s) <= 1).Concat(GetGODHeaders(input)).ToArray();
        }

        if (GODContainerReader.IsGOD(input))
        {
            return new[] { input };
        }
        if (IsImageFile(input) == false)
        {
            LogLine("Error: Input file should be a ZIP, ISO, CCI or GOD header.");
            return Array.Empty<string>();
        }
        // Any part of a split set converts the whole set from part 1.
        return new[] { GetSplitPart(input) > 1 ? ContainerUtility.GetSlicesFromFile(input)[0] : input };
    }

    public static void Process()
    {
        var inputFiles = GetInputFiles();
        var tempFolder = Path.Combine(output, "Temp");

        for (int i = 0; i < inputFiles.Length; i++)
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);

            try
            {

                var originalFile = inputFiles[i];
                var processFile = originalFile;
                var isGod = GODContainerReader.IsGOD(originalFile);
                var isSplit = isGod == false && GetSplitPart(originalFile) == 1;
                var baseName = isGod ? GetGODName(originalFile) : Path.GetFileNameWithoutExtension(originalFile);
                if (isSplit)
                {
                    baseName = Path.GetFileNameWithoutExtension(baseName);
                }

                LogLine($"Processing {baseName} - {i + 1} of {inputFiles.Length}");

                if (Path.GetExtension(processFile).Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
                {
                    processFile = Unpack(processFile);
                    if (processFile == null)
                    {
                        LogLine("Error: Failed extracting archive.");
                        continue;
                    }
                    baseName = Path.GetFileNameWithoutExtension(processFile);
                }

                var extension = format == "CCI" ? ".cci" : ".iso";
                var convertFolder = Path.Combine(tempFolder, "Convert");
                Directory.CreateDirectory(convertFolder);
                var containerPath = Path.Combine(convertFolder, baseName + extension);
                var processExtension = Path.GetExtension(processFile);
                var splitPoint = noSplit ? 0L : FatxSplitPoint;

                using (ContainerReader isoContainer = isGod ? new GODContainerReader(processFile) : processExtension.Equals(".iso", StringComparison.CurrentCultureIgnoreCase) ? new ISOContainerReader(processFile) : new CCIContainerReader(processFile))
                {
                    if (isoContainer.TryMount() == false)
                    {
                        isoContainer.Dispose();
                        LogLine("Error: Failed to mount.");
                        continue;
                    }
                    var xexOptions = GetXexOptions();
                    if (xexOptions.Length > 0 && IsXbox360(isoContainer))
                    {
                        // Converted files can change size, so the disc is extracted, converted and built again.
                        LogLine("Extracting...");
                        var extractFolder = Path.Combine(tempFolder, "Extract");
                        if (isoContainer.TryExtractFiles(extractFolder) == false)
                        {
                            LogLine("Error: Failed extracting files.");
                            continue;
                        }
                        LogLine("Converting XEX files...");
                        XexConverter.ConvertFolder(extractFolder, xexToolPath, xexOptions, Path.Combine(tempFolder, "Xex"));
                        LogLine("Rebuilding...");
                        if (string.Equals(extension, ".cci") && ContainerUtility.ConvertFolderToCCI(extractFolder, ISOFormat.Xbox360, containerPath, splitPoint, null) == false)
                        {
                            LogLine("Error: Failed creating cci.");
                            continue;
                        }
                        if (string.Equals(extension, ".iso") && ContainerUtility.ConvertFolderToISO(extractFolder, ISOFormat.Xbox360, containerPath, splitPoint, null) == false)
                        {
                            LogLine("Error: Failed creating iso.");
                            continue;
                        }
                    }
                    else
                    {
                        if (xexOptions.Length > 0)
                        {
                            LogLine("Warning: not an Xbox 360 disc, XEX options ignored.");
                        }
                        LogLine("Converting...");
                        if (string.Equals(extension, ".cci") && ContainerUtility.ConvertContainerToCCI(isoContainer, ProcessingOptions.All, containerPath, splitPoint, null) == false)
                        {
                            LogLine("Error: Failed creating cci.");
                            continue;
                        }
                        if (string.Equals(extension, ".iso") && ContainerUtility.ConvertContainerToISO(isoContainer, ProcessingOptions.All, containerPath, splitPoint, null) == false)
                        {
                            LogLine("Error: Failed creating iso.");
                            continue;
                        }
                    }
                }

                // Split output is written as name.1.ext, name.2.ext, ... so move every part.
                foreach (var convertedFile in Directory.GetFiles(convertFolder))
                {
                    File.Move(convertedFile, Path.Combine(output, Path.GetFileName(convertedFile)), true);
                }

                if (delete == true && isGod)
                {
                    LogLine($"Deleting '{Path.GetFileName(originalFile)}' and its data folder.");
                    File.Delete(originalFile);
                    Directory.Delete(originalFile + ".data", true);
                }
                else if (delete == true)
                {
                    foreach (var deleteFile in isSplit ? ContainerUtility.GetSlicesFromFile(originalFile) : new[] { originalFile })
                    {
                        LogLine($"Deleting '{Path.GetFileName(deleteFile)}'.");
                        File.Delete(deleteFile);
                    }
                }

            }
            catch (Exception ex)
            {
                LogLine($"Error: {ex}");
            }
        }

        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
        LogLine("Done!");
    }

    private static void Main(string[] args)
    {
        var options = new OptionSet {
            { "i|input=", "a folder of ISO/CCI/ZIP's and GOD containers, or a single file.", i => input = i },
            { "o|output=", "the destination path of ISO/CCI's.", i => output = i },
            { "f|format=", "the destination format (ISO/CCI) default CCI.", f => format = f },
            { "d|delete", "delete original file after successful processing.", v => delete = v != null },
            { "n|nosplit", "do not split output at the FATX 4GB file size limit.", v => noSplit = v != null },
            { "m|machine=", "re-sign every XEX, including those in STFS packages, for DEVKIT or RETAIL.", m => machine = m },
            { "u|unrestrict", "remove media, region and other limits from every XEX, including those in STFS packages.", v => unrestrict = v != null },
            { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
        };

        try
        {
            List<string> extra = options.Parse(args);

            if (shouldShowHelp || args.Length == 0)
            {
                Console.WriteLine("Xbox360Convert: ");
                options.WriteOptionDescriptions(System.Console.Out);
                return;
            }

            if (string.IsNullOrEmpty(input) == true)
            {
                throw new OptionException("input path is invalid", "input");
            }

            input = Path.GetFullPath(input);
            if (Directory.Exists(input) == false && File.Exists(input) == false)
            {
                throw new OptionException("input path does not exist.", "input");
            }

            if (string.IsNullOrEmpty(output) == true)
            {
                throw new OptionException("output path is invalid", "output");
            }

            output = Path.GetFullPath(output);
            if (Directory.Exists(output) == false)
            {
                throw new OptionException("output path does not exist.", "output");
            }

            var inputFolder = File.Exists(input) ? Path.GetDirectoryName(input) : input;
            if (string.Equals(inputFolder, output, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new OptionException("output path should not be same as input.", "output");
            }

            if ((format.Equals("ISO", StringComparison.CurrentCultureIgnoreCase) || format.Equals("CCI", StringComparison.CurrentCultureIgnoreCase)) == false)
            {
                throw new OptionException("format should be ISO or CCI.", "format");
            }

            if (machine.Length > 0 && (machine.Equals("DEVKIT", StringComparison.OrdinalIgnoreCase) || machine.Equals("RETAIL", StringComparison.OrdinalIgnoreCase)) == false)
            {
                throw new OptionException("machine should be DEVKIT or RETAIL.", "machine");
            }

            if (machine.Length > 0 || unrestrict)
            {
                try
                {
                    xexToolPath = XexTool.Find() ?? XexTool.Download();
                }
                catch (Exception ex)
                {
                    throw new OptionException($"XexTool was not found and could not be downloaded: {ex.Message}", "machine");
                }
            }

            Process();
        }
        catch (OptionException e)
        {
            Console.Write("Xbox360Convert: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `xbox360convert --help' for more information.");
            return;
        }
    }
}