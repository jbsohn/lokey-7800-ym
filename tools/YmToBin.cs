#!/usr/bin/env dotnet-script
#nullable enable
using System.Diagnostics;

// YM to Atari 7800 Binary Converter (Pattern-Based Delta)
// ------------------------------------------------------
// This tool converts Atari ST YM files into a custom binary format optimized for the 6502.
// DEPENDENCIES: Requires '7z' (7-Zip) to be installed and in the system PATH for extracting .ym archives.
//
// It performs three key optimizations:
// 1. PITCH SCALING: Adjusts the YM chip clock (2.0MHz) to the Atari 7800 PHI2 (1.79MHz).
// 2. DELTA MASKING: Only stores the registers that actually change between frames.
// 3. PATTERN DEDUPLICATION: Slices the song into blocks and deduplicates identical ones.
//
// BINARY FORMAT:
// [1 byte] Pattern Size (frames per block)
// [1 byte] Unique Pattern Count
// [1 byte] Sequence Length
// [Sequence Data...] IDs of patterns in order
// [Offset Table...] 16-bit relative pointers to unique pattern data
// [Pattern Data...] Delta-masked frame data: [2-byte Mask] [Changed Bytes...]

#pragma warning disable CA1050 

var commandLineArgs = Environment.GetCommandLineArgs().Skip(2).ToArray();
return YMConverter.Run(commandLineArgs);

public class YMHeader
{
    public string Signature { get; set; } = string.Empty;
    public int TotalFrames { get; set; }
    public int ChipClock { get; set; }
    public int PlayerHz { get; set; }
    public int DataOffset { get; set; }
}

public class YMConverter
{
    private const int NumRegisters = 14;
    private const double Atari7800Clock = 1.792000;
    private const int DefaultPatternSize = 64;

    /// <summary>
    /// The entry point for the YM conversion process. Parses arguments and orchestrates the conversion.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>0 on success, 1 on error.</returns>
    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("Usage: dotnet script YmToBin.cs <input.ym> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o <file>      Output binary file (default: input.bin)");
            Console.WriteLine("  -f <frames>    Maximum frames to process (default: 65535)");
            Console.WriteLine("  -p <size>      Pattern block size (default: 0 for auto)");
            Console.WriteLine("  -s <step>      Frame step/skip (default: 1)");
            Console.WriteLine("  -hz <value>    Override PlayerHz (default: from YM header)");
            return 1;
        }

        string inputFile = args[0];
        string? outputFile = null;
        int maxFramesInput = 65535;
        int patternSize = 0;
        int step = 1;
        int? overrideHz = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o": if (i + 1 < args.Length) outputFile = args[++i]; break;
                case "-f": if (i + 1 < args.Length) maxFramesInput = int.Parse(args[++i]); break;
                case "-p": if (i + 1 < args.Length) patternSize = int.Parse(args[++i]); break;
                case "-s": if (i + 1 < args.Length) step = int.Parse(args[++i]); break;
                case "-hz": if (i + 1 < args.Length) overrideHz = int.Parse(args[++i]); break;
            }
        }

        if (string.IsNullOrEmpty(outputFile))
            outputFile = Path.ChangeExtension(inputFile, ".bin");

        string configFile = Path.ChangeExtension(outputFile, ".yminc");

        // Pre-flight check: Verify 7-Zip is installed (only if input is likely an archive)
        if (!IsToolInstalled("7z"))
        {
            Console.WriteLine("WARNING: '7z' (7-Zip) is not installed or not in your PATH.");
            Console.WriteLine("This tool is required to extract .ym archives. Raw .ym files will still work.");
        }

        try
        {
            byte[] rawData = ExtractRawData(inputFile);
            YMHeader header = ParseHeader(rawData);

            if (overrideHz.HasValue)
            {
                Console.WriteLine($"Overriding PlayerHz: {header.PlayerHz} -> {overrideHz.Value}");
                header.PlayerHz = overrideHz.Value;
            }

            int framesToProcess = Math.Min(header.TotalFrames, maxFramesInput);
            int effectiveHz = header.PlayerHz / step;

            Console.WriteLine($"Processing {inputFile} -> {outputFile}");
            Console.WriteLine($"Format: {header.Signature} | Total Frames: {header.TotalFrames} | Step: {step} ({effectiveHz}Hz)");

            byte[] interleavedData = DeinterleaveAndScale(rawData, header, framesToProcess, step);
            int outputFrames = interleavedData.Length / NumRegisters;

            byte[]? bestData = null;
            int bestSize = 0;
            int bestUnique = 0;
            int bestSeq = 0;

            if (patternSize > 0)
            {
                bestData = CompressWithPatterns(interleavedData, outputFrames, patternSize, out bestUnique, out bestSeq);
                bestSize = patternSize;
            }
            else
            {
                Console.WriteLine("Optimizing pattern size...");
                int[] candidates = { 16, 32, 48, 64, 80, 96, 128, 160, 192, 256 };
                foreach (int size in candidates)
                {
                    byte[] trial = CompressWithPatterns(interleavedData, outputFrames, size, out int u, out int s);
                    Console.WriteLine($"  Size {size,3}: {trial.Length,6} bytes ({u,3} unique patterns)");
                    if (bestData == null || trial.Length < bestData.Length)
                    {
                        bestData = trial;
                        bestSize = size;
                        bestUnique = u;
                        bestSeq = s;
                    }
                }
            }

            int yDelay = CalculateDelay(effectiveHz, out int xFine);
            SaveOutput(outputFile, configFile, inputFile, bestData, header, outputFrames, yDelay, xFine, bestUnique, bestSeq, bestSize, effectiveHz);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Checks if a command-line tool is installed and accessible in the system PATH.
    /// </summary>
    /// <param name="toolName">The name of the executable to check.</param>
    /// <returns>True if the tool is found, false otherwise.</returns>
    private static bool IsToolInstalled(string toolName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = toolName,
                Arguments = "", // No arguments, just checking for existence
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the input file and decompresses it if it's an archive (like LZH).
    /// </summary>
    /// <param name="filePath">Path to the .ym file.</param>
    /// <returns>Raw byte array of the YM file.</returns>
    private static byte[] ExtractRawData(string filePath)
    {
        byte[] buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 2) == "YM")
        {
            return buffer;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "7z",
            Arguments = $"x -so \"{filePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi) ?? throw new Exception("Failed to start 7z process.");
        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        byte[] data = ms.ToArray();
        if (data.Length < 34) throw new Exception("Extracted file too small or 7z failed.");
        return data;
    }

    /// <summary>
    /// Parses the YM file header to extract chip clock, player rate, and data offset.
    /// </summary>
    /// <param name="data">The raw YM file bytes.</param>
    /// <returns>A YMHeader object with the parsed metadata.</returns>
    private static YMHeader ParseHeader(byte[] data)
    {
        string signature = System.Text.Encoding.ASCII.GetString(data, 0, 4);
        var header = new YMHeader { Signature = signature };
        if (signature == "YM2!" || signature == "YM3!")
        {
            header.DataOffset = 4;
            header.TotalFrames = (data.Length - 4) / NumRegisters;
            header.ChipClock = 2000000;
            header.PlayerHz = 50;
        }
        else if (signature == "YM4!" || signature == "YM5!" || signature == "YM6!")
        {
            header.TotalFrames = (data[12] << 24) | (data[13] << 16) | (data[14] << 8) | data[15];
            header.ChipClock = (data[22] << 24) | (data[23] << 16) | (data[24] << 8) | data[25];
            header.PlayerHz = (data[26] << 8) | data[27];
            int skip = 34;
            int digidrums = (data[20] << 8) | data[21];
            if (digidrums > 0)
            {
                for (int d = 0; d < digidrums; d++)
                {
                    int dSize = (data[skip] << 24) | (data[skip + 1] << 16) | (data[skip + 2] << 8) | data[skip + 3];
                    skip += 4 + dSize;
                }
            }
            for (int i = 0; i < 3; i++) { while (data[skip] != 0) skip++; skip++; }
            header.DataOffset = skip;
        }
        else throw new Exception($"Unsupported format '{signature}'");
        return header;
    }

    /// <summary>
    /// Reorders the YM's register-first format into a frame-first format
    /// and scales the pitches to match the 7800's slightly slower CPU clock.
    /// </summary>
    private static byte[] DeinterleaveAndScale(byte[] rawData, YMHeader header, int framesToProcess, int step)
    {
        int outputFrames = 0;
        for (int i = 0; i < framesToProcess; i += step) outputFrames++;

        byte[] interleavedData = new byte[outputFrames * NumRegisters];
        double pitchScale = Atari7800Clock / (header.ChipClock / 1000000.0);

        for (int f = 0; f < outputFrames; f++)
        {
            int sourceFrame = f * step;
            byte[] frame = new byte[NumRegisters];
            for (int r = 0; r < NumRegisters; r++)
            {
                // YM files store all R0 bytes, then all R1 bytes. We need them interleaved per frame.
                int offset = header.DataOffset + (r * header.TotalFrames) + sourceFrame;
                frame[r] = rawData[offset];
            }

            // --- PITCH SCALING ---
            // The 7800 PHI2 clock is roughly 1.79MHz while the Atari ST YM clock is 2.0MHz.
            // Without scaling, the notes would sound out of tune.
            int Scale(int low, int hi, int mask)
            {
                int val = ((hi & mask) << 8) | low;
                return (int)Math.Round(val * pitchScale);
            }
            int tA = Scale(frame[0], frame[1], 0x0F);
            frame[0] = (byte)(tA & 0xFF); frame[1] = (byte)((tA >> 8) & 0x0F);
            int tB = Scale(frame[2], frame[3], 0x0F);
            frame[2] = (byte)(tB & 0xFF); frame[3] = (byte)((tB >> 8) & 0x0F);
            int tC = Scale(frame[4], frame[5], 0x0F);
            frame[4] = (byte)(tC & 0xFF); frame[5] = (byte)((tC >> 8) & 0x0F);
            int n = (int)Math.Round((frame[6] & 0x1F) * pitchScale);
            frame[6] = (byte)(n & 0x1F);
            int e = Scale(frame[11], frame[12], 0xFF);
            frame[11] = (byte)(e & 0xFF); frame[12] = (byte)((e >> 8) & 0xFF);

            for (int r = 0; r < NumRegisters; r++)
                interleavedData[(f * NumRegisters) + r] = frame[r];
        }
        return interleavedData;
    }

    /// <summary>
    /// Slices the song into fixed-size blocks (patterns) and identifies identical ones
    /// to store only the unique patterns.
    /// </summary>
    private static byte[] CompressWithPatterns(byte[] interleavedData, int framesToProcess, int patternSize, out int uniqueCount, out int seqLen)
    {
        int numBlocks = (int)Math.Ceiling((double)framesToProcess / patternSize);
        var uniquePatternRaw = new List<byte[]>();
        var sequence = new List<int>();

        for (int b = 0; b < numBlocks; b++)
        {
            int actualFrames = Math.Min(patternSize, framesToProcess - (b * patternSize));
            byte[] block = new byte[actualFrames * NumRegisters];
            Array.Copy(interleavedData, b * patternSize * NumRegisters, block, 0, block.Length);

            // Check if this block already exists
            int patternId = -1;
            for (int i = 0; i < uniquePatternRaw.Count; i++)
            {
                if (uniquePatternRaw[i].SequenceEqual(block)) { patternId = i; break; }
            }
            if (patternId == -1)
            {
                patternId = uniquePatternRaw.Count;
                uniquePatternRaw.Add(block);
            }
            sequence.Add(patternId);
        }

        uniqueCount = uniquePatternRaw.Count;
        seqLen = sequence.Count;

        // Apply Delta-Masking to each unique pattern
        var compressedPatterns = new List<byte[]>();
        foreach (var raw in uniquePatternRaw)
        {
            compressedPatterns.Add(CompressPattern(raw));
        }

        // --- DATA ASSEMBLY ---
        var output = new List<byte>();
        output.Add((byte)patternSize);
        output.Add((byte)uniqueCount);
        output.Add((byte)seqLen);
        foreach (int id in sequence) output.Add((byte)id);

        int currentOffset = 0;
        var offsets = new List<ushort>();
        foreach (var cp in compressedPatterns)
        {
            offsets.Add((ushort)currentOffset);
            currentOffset += cp.Length;
        }

        foreach (var off in offsets)
        {
            output.Add((byte)(off & 0xFF));
            output.Add((byte)((off >> 8) & 0xFF));
        }
        foreach (var cp in compressedPatterns) output.AddRange(cp);

        return [.. output];
    }

    /// <summary>
    /// Performs Delta-Masking: Compares each frame to the previous one and
    /// generates a bitmask to indicate which registers changed.
    /// </summary>
    private static byte[] CompressPattern(byte[] rawData)
    {
        int frames = rawData.Length / NumRegisters;
        List<byte> compressed = new List<byte>();
        byte[] lastFrame = new byte[NumRegisters];

        for (int f = 0; f < frames; f++)
        {
            ushort mask = 0;
            List<byte> frameData = new List<byte>();
            for (int r = 0; r < NumRegisters; r++)
            {
                byte current = rawData[(f * NumRegisters) + r];
                // Only store a register if its value changed (or if it's the very first frame)
                if (f == 0 || current != lastFrame[r])
                {
                    mask |= (ushort)(1 << r);
                    frameData.Add(current);
                }
                lastFrame[r] = current;
            }
            // Store the 16-bit mask (low byte first) followed by only the changed register values
            compressed.Add((byte)(mask & 0xFF));
            compressed.Add((byte)((mask >> 8) & 0xFF));
            compressed.AddRange(frameData);
        }
        return [.. compressed];
    }

    /// <summary>
    /// Calculates the 6502 delay loop values (Y and X) needed to achieve the target playback rate.
    /// accounts for an estimated player overhead of 1800 cycles.
    /// </summary>
    /// <param name="playerHz">The target frequency (e.g. 50Hz, 60Hz).</param>
    /// <param name="fine">Output: The X-loop (fine) delay value (0-255).</param>
    /// <returns>The Y-loop (coarse) delay value.</returns>
    private static int CalculateDelay(int playerHz, out int fine)
    {
        // Atari 7800 NTSC PHI2 = 1.789773 MHz
        double ntscClock = 1789773.0;
        double targetCycles = ntscClock / playerHz;

        // Estimated Player Overhead (fetching patterns, writing YM)
        double playerOverhead = 1800.0;
        double remainingCycles = Math.Max(0, targetCycles - playerOverhead);

        // Coarse Delay (Y Loop, 1285 cycles per step)
        int yDelay = (int)Math.Floor(remainingCycles / 1285.0);
        remainingCycles -= (yDelay * 1285.0);

        // Fine Delay (X Loop, 5 cycles per step)
        fine = (int)Math.Round(remainingCycles / 5.0);
        if (fine > 255) fine = 255;

        return (int)Math.Max(1, yDelay);
    }

    /// <summary>
    /// Saves the compressed binary data and the generated assembly include file.
    /// </summary>
    /// <param name="binPath">Path to write the .bin file.</param>
    /// <param name="incPath">Path to write the .yminc file.</param>
    /// <param name="inputFile">The original input filename (for comments).</param>
    /// <param name="data">The compressed binary data bytes.</param>
    /// <param name="header">The YM metadata header.</param>
    /// <param name="frames">Total number of frames in the output.</param>
    /// <param name="yDelay">The calculated coarse delay for the 6502.</param>
    /// <param name="xFine">The calculated fine delay for the 6502.</param>
    /// <param name="uniqueCount">Number of unique patterns identified.</param>
    /// <param name="seqLen">Length of the pattern sequence.</param>
    /// <param name="patternSize">Number of frames per pattern block.</param>
    /// <param name="playerHz">The final playback frequency in Hz.</param>
    private static void SaveOutput(string binPath, string incPath, string inputFile, byte[]? data, YMHeader header, int frames, int yDelay, int xFine, int uniqueCount, int seqLen, int patternSize, int playerHz)
    {
        if (data == null)
        {
            Console.WriteLine("ERROR: input data is empty.");
            return;
        }

        File.WriteAllBytes(binPath, data);
        using (var sw = new StreamWriter(incPath))
        {
            sw.WriteLine($"; Generated config for {inputFile}");
            sw.WriteLine($"MAX_FRAMES   = {frames}");
            sw.WriteLine($"PLAYER_HZ    = {playerHz}");
            sw.WriteLine($"YM_DELAY     = {yDelay}");
            sw.WriteLine($"YM_FINE      = {xFine}");
            sw.WriteLine($"PATTERN_SIZE = {patternSize}");
            sw.WriteLine($"NUM_PATTERNS = {uniqueCount}");
            sw.WriteLine($"SEQ_LEN      = {seqLen}");
        }
        Console.WriteLine($"Wrote {data.Length} bytes to {binPath}");
        Console.WriteLine($"Calculated Delay: Y={yDelay}, X={xFine} for {playerHz}Hz");
        double originalSize = frames * NumRegisters;
        Console.WriteLine($"Compression Ratio: {data.Length / originalSize * 100:F1}%");
    }
}
