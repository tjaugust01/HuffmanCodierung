using System.Diagnostics;

namespace HuffmanCodierung;

public class App
{
    public string FilePath = "test.odt";
    public string OutputHuffPath = string.Empty;
    public byte[] FileBytes = Array.Empty<byte>();
    public byte[] PayloadBytes = Array.Empty<byte>();
    public byte[] CompressedPackageBytes = Array.Empty<byte>();

    private Dictionary<byte, string> _codes = new();
    private Dictionary<byte, int> _freq = new();
    private HuffmanTree _tree = null!;

    public void Run(string[] args)
    {
        string mode = string.Empty;
        string targetFile = string.Empty;
        bool showStatistics = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();
            if (arg.Equals("-c", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--compress", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("c", StringComparison.OrdinalIgnoreCase))
            {
                mode = "c";
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    targetFile = args[++i];
                }
            }
            else if (arg.Equals("-d", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("--decompress", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("d", StringComparison.OrdinalIgnoreCase))
            {
                mode = "d";
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                {
                    targetFile = args[++i];
                }
            }
            else if (arg.Equals("-s", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("--statistic", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                showStatistics = true;
            }
            else if (string.IsNullOrEmpty(targetFile) && !arg.StartsWith('-'))
            {
                targetFile = arg;
            }
        }

        // Falls keine Parameter uebergeben wurden, interaktive Auswahl:
        if (string.IsNullOrEmpty(mode))
        {
            Terminal.info("=== Huffman Codierung CLI ===");
            Terminal.info("Optionen: [c]ompress | [d]ecompress | [s]tatistic");
            string input = Terminal.Prompt("Aktion waehlen: [c]ompress oder [d]ecompress? (c/d): ").ToLower();
            mode = input.StartsWith("d") ? "d" : "c";
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            targetFile = Terminal.Prompt(mode == "c"
                ? "Pfad der zu komprimierenden Datei (z. B. data/test.odt): "
                : "Pfad der zu dekomprimierenden Datei (z. B. data/test.odt.huff): ");
        }

        if (mode == "c" && !showStatistics && args.Length == 0)
        {
            string statChoice = Terminal.Prompt("Soll die Entropie-Statistik (-s) ausgegeben werden? (y/n): ").ToLower();
            showStatistics = statChoice == "y";
        }

        if (mode == "c")
        {
            CompressCommand(targetFile, showStatistics);
        }
        else
        {
            DecompressCommand(targetFile);
        }
    }

    private void CompressCommand(string inputPath, bool showStatistics)
    {
        string resolvedPath = ResolveFilePath(inputPath);
        if (!File.Exists(resolvedPath))
        {
            Terminal.info($"Datei '{inputPath}' nicht gefunden. Erstelle Test-Datei...");
            string dir = Path.GetDirectoryName(resolvedPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(resolvedPath, "Das ist ein Testtext fuer die Huffman-Codierung mit Bytes!");
            Terminal.info($"Testdatei '{resolvedPath}' wurde erstellt.");
        }

        FilePath = resolvedPath;
        FileBytes = File.ReadAllBytes(FilePath);

        if (FileBytes.Length == 0)
        {
            Terminal.error("Die angegebene Datei ist leer.");
            return;
        }

        Terminal.info($"Lese '{Path.GetFileName(FilePath)}' ({FileBytes.Length} Bytes)...");
        BuildHuffmanTree();
        Encode();
        SaveHuffFile();

        if (showStatistics)
        {
            PrintEntropyStatistics();
        }
    }

    private void DecompressCommand(string inputPath)
    {
        string resolvedPath = ResolveFilePath(inputPath);
        if (!File.Exists(resolvedPath))
        {
            // Pruefe zusaetzlich, ob eventuell die Endung .huff weggelassen wurde
            if (File.Exists(resolvedPath + ".huff"))
            {
                resolvedPath = resolvedPath + ".huff";
            }
            else
            {
                Terminal.error($"Die .huff-Datei '{inputPath}' wurde nicht gefunden.");
                return;
            }
        }

        Terminal.info($"Dekomprimiere Datei: {resolvedPath}");
        try
        {
            var (decodedBytes, originalFileName) = DecompressFile(resolvedPath);

            string targetDir = Path.GetDirectoryName(resolvedPath) ?? Directory.GetCurrentDirectory();
            string targetFileName = resolvedPath.EndsWith(".huff", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(resolvedPath[..^5])
                : originalFileName;

            string targetPath = Path.Combine(targetDir, targetFileName);

            // Falls die Originaldatei am Zielort bereits existiert, umbenennen statt ueberschreiben:
            if (File.Exists(targetPath))
            {
                string ext = Path.GetExtension(targetFileName);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(targetFileName);
                targetPath = Path.Combine(targetDir, $"{nameWithoutExt}_decompressed{ext}");
            }

            File.WriteAllBytes(targetPath, decodedBytes);
            Terminal.success("Dekomprimierung erfolgreich abgeschlossen!");
            Terminal.success($"Wiederhergestellte Datei gespeichert: {Path.GetFullPath(targetPath)} ({decodedBytes.Length} Bytes)");
        }
        catch (Exception ex)
        {
            Terminal.error($"Fehler bei der Dekomprimierung: {ex.Message}");
        }
    }

    private void PrintEntropyStatistics()
    {
        int originalBits = FileBytes.Length * 8;
        int compressedBits = FileBytes.Sum(b => _codes[b].Length);
        double entropy = 0.0;
        int totalBytes = FileBytes.Length;

        foreach (var kvp in _freq)
        {
            double p = (double)kvp.Value / totalBytes;
            entropy -= p * Math.Log2(p);
        }

        double avgBitsPerSymbol = (double)compressedBits / totalBytes;
        double theoreticalMinBytes = (entropy * totalBytes) / 8.0;

        // Bit-Entropie des erzeugten komprimierten Bitstroms (Verteilung von 0 und 1)
        long onesCount = 0;
        for (int i = 0; i < compressedBits; i++)
        {
            int byteIdx = i / 8;
            int bitIdx = 7 - (i % 8);
            if (((PayloadBytes[byteIdx] >> bitIdx) & 1) == 1)
            {
                onesCount++;
            }
        }
        long zerosCount = compressedBits - onesCount;
        double p1 = compressedBits > 0 ? (double)onesCount / compressedBits : 0.0;
        double p0 = compressedBits > 0 ? (double)zerosCount / compressedBits : 0.0;
        double bitEntropy = (p0 > 0 && p1 > 0) ? (-p0 * Math.Log2(p0) - p1 * Math.Log2(p1)) : 0.0;

        Terminal.info("\n========================================================");
        Terminal.info("          STATISTIK & ENTROPIE-ANALYSE (-s)             ");
        Terminal.info("========================================================");
        Terminal.info($"Originalgröße:                   {totalBytes} Bytes ({originalBits} Bits)");
        Terminal.info($"Nutzdaten (codierte Bytes):      {PayloadBytes.Length} Bytes ({compressedBits} Bits)");
        Terminal.info($"Gesamtdatei (inkl. Wörterbuch):  {CompressedPackageBytes.Length} Bytes");
        Terminal.info($"Erstellte .huff-Datei:           {OutputHuffPath}");
        Terminal.info($"Ersparnis (reine Nutzdaten):     {(1.0 - (double)PayloadBytes.Length / totalBytes):P2}");
        Terminal.info($"Ersparnis (Gesamtpaket):         {(1.0 - (double)CompressedPackageBytes.Length / totalBytes):P2}");
        Terminal.info("--------------------------------------------------------");
        Terminal.info($"Berechnete Entropie (Shannon H): {entropy:F4} Bits/Symbol");
        Terminal.info($"Tatsächliche Entropie (Code L):  {avgBitsPerSymbol:F4} Bits/Symbol");
        Terminal.info("--------------------------------------------------------");
        Terminal.info($"Entropie des komprimierten Bits: {bitEntropy:F4} Bits/Bit (Ideal: 1.0000)");
        Terminal.info($"Theoretische Minimalgröße:       ~{Math.Ceiling(theoreticalMinBytes)} Bytes");
        Terminal.info("========================================================\n");
    }

    public static string ResolveFilePath(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            return Path.GetFullPath(inputPath);
        }

        string projectRoot = FindProjectRoot();

        string candidate1 = Path.Combine(projectRoot, inputPath);
        if (File.Exists(candidate1))
        {
            return candidate1;
        }

        string candidate2 = Path.Combine(projectRoot, "data", inputPath);
        if (File.Exists(candidate2))
        {
            return candidate2;
        }

        string candidate3 = Path.Combine(Directory.GetCurrentDirectory(), "data", inputPath);
        if (File.Exists(candidate3))
        {
            return candidate3;
        }

        // Falls noch nicht existent, bevorzugt im Projektordner oder data-Ordner anlegen
        if (inputPath.StartsWith("data/", StringComparison.OrdinalIgnoreCase) || 
            inputPath.StartsWith("data\\", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(projectRoot, inputPath);
        }

        return Path.Combine(projectRoot, "data", inputPath);
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private void Encode()
    {
        Terminal.info("Erstelle codierte Bytes (Bit-Packing)...");

        int compressedBits = FileBytes.Sum(b => _codes[b].Length);
        int payloadSize = (compressedBits + 7) / 8;
        PayloadBytes = new byte[payloadSize];

        string[] codeTable = new string[256];
        foreach (var (b, code) in _codes)
        {
            codeTable[b] = code;
        }

        int outIndex = 0;
        byte currentByte = 0;
        int bitCount = 0;

        for (int i = 0; i < FileBytes.Length; i++)
        {
            string code = codeTable[FileBytes[i]];
            for (int j = 0; j < code.Length; j++)
            {
                currentByte = (byte)((currentByte << 1) | (code[j] == '1' ? 1 : 0));
                bitCount++;
                if (bitCount == 8)
                {
                    PayloadBytes[outIndex++] = currentByte;
                    currentByte = 0;
                    bitCount = 0;
                }
            }
        }

        if (bitCount > 0)
        {
            PayloadBytes[outIndex++] = (byte)(currentByte << (8 - bitCount));
        }

        Terminal.success($"Codierte Bytes erfolgreich erstellt! ({PayloadBytes.Length} Bytes generiert)");

        CreateCompressedPackage();
    }

    private void CreateCompressedPackage()
    {
        Terminal.info("Packe Wörterbuch und Nutzdaten in .huff-Dateiformat...");
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((byte)'H');
        writer.Write((byte)'U');
        writer.Write((byte)'F');
        writer.Write((byte)'F');

        writer.Write(Path.GetFileName(FilePath));

        writer.Write((long)FileBytes.Length);

        writer.Write(_codes.Count);
        foreach (var pair in _codes)
        {
            writer.Write(pair.Key);   
            writer.Write(pair.Value);  
        }

        writer.Write(PayloadBytes);

        CompressedPackageBytes = ms.ToArray();
        Terminal.success($"Archivpaket fertiggestellt (Größe inkl. Wörterbuch: {CompressedPackageBytes.Length} Bytes).");
    }

    private void SaveHuffFile()
    {
        OutputHuffPath = FilePath + ".huff";
        File.WriteAllBytes(OutputHuffPath, CompressedPackageBytes);
        Terminal.success($"--> .huff-Datei wurde erfolgreich erstellt: {Path.GetFullPath(OutputHuffPath)}");
    }

    public static (byte[] DecodedBytes, string OriginalFileName) DecompressFile(string huffFilePath)
    {
        using var fs = File.OpenRead(huffFilePath);
        using var reader = new BinaryReader(fs);

        byte[] magic = reader.ReadBytes(4);
        if (magic.Length < 4 || magic[0] != 'H' || magic[1] != 'U' || magic[2] != 'F' || magic[3] != 'F')
        {
            throw new InvalidDataException("Ungültiges Dateiformat (Keine HUFF-Datei).");
        }

        string originalFileName = reader.ReadString();
        long originalLength = reader.ReadInt64();
        
        int dictCount = reader.ReadInt32();
        var codes = new Dictionary<byte, string>();
        for (int i = 0; i < dictCount; i++)
        {
            byte symbol = reader.ReadByte();
            string code = reader.ReadString();
            codes[symbol] = code;
        }

        var tree = HuffmanTree.FromDictionary(codes);

        int payloadLength = (int)(fs.Length - fs.Position);
        byte[] payload = reader.ReadBytes(payloadLength);

        byte[] decoded = tree.Decode(payload, originalLength);
        return (decoded, originalFileName);
    }

    private void BuildHuffmanTree()
    {
        Terminal.info("Analysiert Byte Häufigkeit...");
        _freq = FileBytes
            .GroupBy(b => b)
            .ToDictionary(g => g.Key, g => g.Count());
        Terminal.info("Erstelle Huffman Baum...");
        _tree = new HuffmanTree(_freq);
        _codes = new Dictionary<byte, string>();

        Terminal.info("Generiert Binary Code");
        _tree.GenerateCodes(_tree.Root, "", _codes);
    }
}
