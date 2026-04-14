#!/usr/bin/env dotnet-script
# nullable enable
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;

// YM to Atari 7800 Binary Converter (Pattern-Based Delta)
// ------------------------------------------------------
// Optimized binary format for 6502 playback.
// Performs pitch scaling, delta-masking, and O(N) pattern deduplication.
var arguments = Environment.GetCommandLineArgs().Skip(2).ToArray();
return YmConverter.Run(arguments);

/// <summary>
/// Represents the metadata header of a YM music file.
/// </summary>
/// <param name="Signature">The 4-character format signature (e.g., "YM6!").</param>
/// <param name="TotalFrames">Total number of audio frames in the file.</param>
/// <param name="ChipClock">The original YM2149 clock frequency (typically 2,000,000 Hz).</param>
/// <param name="PlayerHz">The intended playback rate (typically 50 Hz or 60 Hz).</param>
/// <param name="DataOffset">The byte offset where the interleaved register data begins.</param>
internal record YmHeader(
    string Signature,
    int TotalFrames,
    int ChipClock,
    int PlayerHz,
    int DataOffset
);

/// <summary>
/// Represents the parsed command-line options for the conversion process.
/// </summary>
/// <param name="InputFile">The path to the input .ym file.</param>
/// <param name="OutputFile">The path to the output .bin file (optional).</param>
/// <param name="MaxFrames">Maximum number of frames to process.</param>
/// <param name="PatternSize">Number of frames per pattern block (0 for auto).</param>
/// <param name="Step">Frame skip/step interval.</param>
/// <param name="OverrideHz">Manual playback frequency override (optional).</param>
internal record struct ConversionOptions(
    string InputFile,
    string? OutputFile,
    int MaxFrames,
    int PatternSize,
    int Step,
    int? OverrideHz
);

/// <summary>
/// Orchestrates the conversion of Atari ST YM files into optimized Atari 7800 binaries.
/// </summary>
internal static class YmConverter
{
    private const int NumRegisters = 14;
    private const double Atari7800Clock = 1.792000;

    /// <summary>
    /// The main entry point for the conversion process.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the script.</param>
    /// <returns>Exit code: 0 for success, 1 for failure.</returns>
    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        var inputFile = options.InputFile;
        var outputFile = options.OutputFile ?? Path.ChangeExtension(inputFile, ".bin");
        var configFile = Path.ChangeExtension(outputFile, ".yminc");

        if (!IsToolInstalled("7z"))
            Console.WriteLine("WARNING: '7z' (7-Zip) not found. Required for compressed .ym files.");

        try
        {
            var rawData = ExtractRawData(inputFile);
            var header = ParseHeader(rawData);

            var overrideHz = options.OverrideHz;
            if (overrideHz.HasValue)
            {
                Console.WriteLine($"Overriding PlayerHz: {header.PlayerHz} -> {overrideHz.Value}");
                header = header with { PlayerHz = overrideHz.Value };
            }

            var framesToProcess = Math.Min(header.TotalFrames, options.MaxFrames);
            var effectiveHz = header.PlayerHz / options.Step;

            Console.WriteLine($"Processing {inputFile} -> {outputFile}");
            Console.WriteLine(
                $"Format: {header.Signature} | Frames: {header.TotalFrames} | Step: {options.Step} ({effectiveHz}Hz)");

            var interleavedData = DeinterleaveAndScale(rawData, header, framesToProcess, options.Step);
            var outputFrames = interleavedData.Length / NumRegisters;

            var (bestData, bestSize, bestUnique, bestSeq) =
                OptimizeCompression(interleavedData, outputFrames, options.PatternSize);

            var yDelay = CalculateDelay(effectiveHz, out var xFine);
            SaveOutput(outputFile, configFile, inputFile, bestData, outputFrames, yDelay, xFine, bestUnique, bestSeq,
                bestSize, effectiveHz);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Parses the command-line arguments into a typed options structure.
    /// </summary>
    /// <param name="args">The raw string array of arguments.</param>
    /// <returns>A <see cref="ConversionOptions"/> instance containing parsed parameters.</returns>
    private static ConversionOptions ParseArgs(string[] args)
    {
        var input = args[0];
        string? output = null;
        int max = ushort.MaxValue, pat = 0, step = 1;
        int? hz = null;

        for (var i = 1; i < args.Length; i++)
        {
            if (i + 1 >= args.Length) continue;

            switch (args[i])
            {
                case "-o": output = args[++i]; break;
                case "-f": max = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "-p": pat = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "-s": step = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "-hz": hz = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
            }
        }

        return new ConversionOptions(input, output, max, pat, step, hz);
    }

    /// <summary>
    /// Prints the command-line help text to the console.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet script YmToBin.cs <input.ym> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

    /// <summary>
    /// Verifies if a specific command-line tool is available in the system PATH.
    /// </summary>
    /// <param name="toolName">The name of the executable to find.</param>
    /// <returns>True if the tool is installed, false otherwise.</returns>
    private static bool IsToolInstalled(string toolName)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("whereis", toolName)
            { RedirectStandardOutput = true, UseShellExecute = false });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the input YM file, automatically decompressing it using 7-Zip if necessary.
    /// </summary>
    /// <param name="filePath">The path to the .ym file on disk.</param>
    /// <returns>A byte array containing the raw, uncompressed YM file data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if 7z fails to start.</exception>
    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'Y' && buffer[1] == 'M') return buffer;

        using var process = Process.Start(new ProcessStartInfo("7z", $"x -so \"{filePath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start '7z'. Ensure 7-Zip is installed and in your PATH.");

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        return ms.ToArray();
    }

    /// <summary>
    /// Parses the YM file header to extract metadata like chip clock and frame count.
    /// </summary>
    /// <param name="data">The raw bytes of the uncompressed YM file.</param>
    /// <returns>A populated <see cref="YmHeader"/> record.</returns>
    /// <exception cref="InvalidDataException">Thrown if the format is unsupported or invalid.</exception>
    private static YmHeader ParseHeader(ReadOnlySpan<byte> data)
    {
        var sig = Encoding.ASCII.GetString(data[..4]);

        if (sig is "YM2!" or "YM3!")
            return new YmHeader(sig, (data.Length - 4) / NumRegisters, 2000000, 50, 4);

        if (sig is not ("YM4!" or "YM5!" or "YM6!"))
            throw new InvalidDataException($"Unsupported YM format: {sig}. This tool requires YM4, YM5, or YM6.");

        var frames = BinaryPrimitives.ReadInt32BigEndian(data[12..16]);
        var clock = BinaryPrimitives.ReadInt32BigEndian(data[22..26]);
        int hz = BinaryPrimitives.ReadInt16BigEndian(data[26..28]);
        int digidrums = BinaryPrimitives.ReadInt16BigEndian(data[20..22]);

        var skip = 34;

        while (digidrums-- > 0)
        {
            var drumLength = BinaryPrimitives.ReadInt32BigEndian(data[skip..(skip + 4)]);
            skip += 4 + drumLength;
        }

        for (var i = 0; i < 3; i++)
        {
            while (data[skip] != 0) skip++;
            skip++;
        }

        return new YmHeader(sig, frames, clock, hz, skip);
    }

    /// <summary>
    /// Reorders the "register-first" YM data into a "frame-first" layout while scaling pitches for the 7800 PHI2 clock.
    /// </summary>
    /// <param name="rawData">The raw uncompressed YM file data.</param>
    /// <param name="header">The parsed YM metadata header.</param>
    /// <param name="framesToProcess">Number of frames to extract.</param>
    /// <param name="step">Frame skip/step interval (1 for every frame).</param>
    /// <returns>An interleaved byte array: [F0-R0, F0-R1... F0-R13, F1-R0...].</returns>
    private static byte[] DeinterleaveAndScale(byte[] rawData, YmHeader header, int framesToProcess, int step)
    {
        var outputFrames = (framesToProcess + step - 1) / step;
        var interleavedData = new byte[outputFrames * NumRegisters];
        var pitchScale = Atari7800Clock / (header.ChipClock / 1000000.0);

        Span<byte> frame = stackalloc byte[NumRegisters];
        for (var f = 0; f < outputFrames; f++)
        {
            var sourceFrame = f * step;
            for (var r = 0; r < NumRegisters; r++)
                frame[r] = rawData[header.DataOffset + r * header.TotalFrames + sourceFrame];

            var tA = Scale(frame[0], frame[1], 0x0F, pitchScale);
            frame[0] = (byte)(tA & 0xFF);
            frame[1] = (byte)((tA >> 8) & 0x0F);
            var tB = Scale(frame[2], frame[3], 0x0F, pitchScale);
            frame[2] = (byte)(tB & 0xFF);
            frame[3] = (byte)((tB >> 8) & 0x0F);
            var tC = Scale(frame[4], frame[5], 0x0F, pitchScale);
            frame[4] = (byte)(tC & 0xFF);
            frame[5] = (byte)((tC >> 8) & 0x0F);
            frame[6] = (byte)((int)Math.Round((frame[6] & 0x1F) * pitchScale) & 0x1F);
            var e = Scale(frame[11], frame[12], 0xFF, pitchScale);
            frame[11] = (byte)(e & 0xFF);
            frame[12] = (byte)((e >> 8) & 0xFF);

            frame.CopyTo(interleavedData.AsSpan(f * NumRegisters));
            continue;

            static int Scale(int low, int hi, int mask, double scale)
            {
                return (int)Math.Round((((hi & mask) << 8) | low) * scale);
            }
        }

        return interleavedData;
    }

    /// <summary>
    /// Tries various pattern sizes to find the most efficient compression ratio for the given song.
    /// </summary>
    /// <param name="interleavedData">The frame-first register data.</param>
    /// <param name="frames">Total number of frames.</param>
    /// <param name="manualPatSize">If > 0, skips optimization and uses this specific size.</param>
    /// <returns>A tuple containing the best compressed binary data and its compression metadata.</returns>
    private static (byte[] data, int size, int unique, int seq) OptimizeCompression(byte[] interleavedData, int frames,
        int manualPatSize)
    {
        if (manualPatSize > 0)
        {
            var data = CompressWithPatterns(interleavedData, frames, manualPatSize, out var u, out var s);
            return (data, manualPatSize, u, s);
        }

        Console.WriteLine("Optimizing pattern size...");
        byte[]? bestData = null;
        int bestSize = 0, bestU = 0, bestS = 0;
        int[] patternSizes = [16, 32, 48, 64, 80, 96, 128, 160, 192, 256];
        foreach (var size in patternSizes)
        {
            var trial = CompressWithPatterns(interleavedData, frames, size, out var u, out var s);
            Console.WriteLine($"  Size {size,3}: {trial.Length,6} bytes ({u,3} unique)");
            if (bestData == null || trial.Length < bestData.Length)
                (bestData, bestSize, bestU, bestS) = (trial, size, u, s);
        }

        return (bestData!, bestSize, bestU, bestS);
    }

    /// <summary>
    /// Slices the song into fixed-size blocks (patterns) and identifies identical ones using O(N) deduplication.
    /// </summary>
    /// <param name="interleavedData">The frame-first register data.</param>
    /// <param name="totalFrames">Total frames in the song.</param>
    /// <param name="patSize">Frames per pattern block.</param>
    /// <param name="uniqueCount">Output: Number of unique patterns discovered.</param>
    /// <param name="seqLen">Output: Length of the pattern sequence table.</param>
    /// <returns>A byte array containing the full compressed binary structure.</returns>
    private static byte[] CompressWithPatterns(byte[] interleavedData, int totalFrames, int patSize,
        out int uniqueCount, out int seqLen)
    {
        var numBlocks = (int)Math.Ceiling((double)totalFrames / patSize);
        var uniquePatterns = new List<byte[]>();
        var sequence = new List<int>();
        var lookup = new Dictionary<byte[], int>(new ByteArrayComparer());

        for (var b = 0; b < numBlocks; b++)
        {
            var framesInBlock = Math.Min(patSize, totalFrames - b * patSize);
            var block = new byte[framesInBlock * NumRegisters];
            Array.Copy(interleavedData, b * patSize * NumRegisters, block, 0, block.Length);

            if (!lookup.TryGetValue(block, out var id))
            {
                id = uniquePatterns.Count;
                uniquePatterns.Add(block);
                lookup[block] = id;
            }

            sequence.Add(id);
        }

        uniqueCount = uniquePatterns.Count;
        seqLen = sequence.Count;

        List<byte> output = [(byte)patSize, (byte)uniqueCount, (byte)seqLen];
        output.AddRange(sequence.Select(id => (byte)id));

        var compressed = uniquePatterns.Select(CompressPattern).ToList();
        var offset = 0;
        foreach (var cp in compressed)
        {
            output.Add((byte)(offset & 0xFF));
            output.Add((byte)((offset >> 8) & 0xFF));
            offset += cp.Length;
        }

        output.AddRange(compressed.SelectMany(x => x));
        return output.ToArray();
    }

    /// <summary>
    /// Performs delta-masking on a single pattern: only stores registers that changed relative to the previous frame.
    /// </summary>
    /// <param name="raw">The raw interleaved register data for this pattern.</param>
    /// <returns>Delta-compressed bytes: [2-byte Mask][Changed Bytes...].</returns>
    private static byte[] CompressPattern(byte[] raw)
    {
        var frames = raw.Length / NumRegisters;
        var compressed = new List<byte>();
        var last = new byte[NumRegisters];

        for (var f = 0; f < frames; f++)
        {
            ushort mask = 0;
            var frameData = new List<byte>();
            for (var r = 0; r < NumRegisters; r++)
            {
                var current = raw[f * NumRegisters + r];
                if (f == 0 || current != last[r])
                {
                    mask |= (ushort)(1 << r);
                    frameData.Add(current);
                }

                last[r] = current;
            }

            compressed.Add((byte)(mask & 0xFF));
            compressed.Add((byte)((mask >> 8) & 0xFF));
            compressed.AddRange(frameData);
        }

        return compressed.ToArray();
    }

    /// <summary>
    /// Calculates the 6502 delay loop constants needed to hit the target playback rate, accounting for overhead.
    /// </summary>
    /// <param name="hz">Target player frequency (e.g., 50 or 60).</param>
    /// <param name="fine">Output: The fine-tuning (X-loop) constant.</param>
    /// <returns>The coarse-delay (Y-loop) constant.</returns>
    private static int CalculateDelay(int hz, out int fine)
    {
        var remaining = Math.Max(0, 1789773.0 / hz - 1800.0);
        var y = (int)Math.Floor(remaining / 1285.0);
        fine = (int)Math.Min(255, Math.Round((remaining - y * 1285.0) / 5.0));
        return Math.Max(1, y);
    }

    /// <summary>
    /// Writes the final binary data and the generated assembly configuration file to disk.
    /// </summary>
    /// <param name="bin">The path for the .bin file.</param>
    /// <param name="inc">The path for the .yminc assembly include file.</param>
    /// <param name="src">The original source filename (for comments).</param>
    /// <param name="data">The compressed binary data.</param>
    /// <param name="frames">Total frames in the output.</param>
    /// <param name="y">Calculated coarse delay loop value.</param>
    /// <param name="x">Calculated fine delay loop value.</param>
    /// <param name="u">Number of unique patterns.</param>
    /// <param name="s">Length of the pattern sequence.</param>
    /// <param name="p">Frames per pattern block.</param>
    /// <param name="hz">The playback frequency in Hz.</param>
    private static void SaveOutput(string bin, string inc, string src, byte[] data, int frames, int y, int x, int u,
        int s, int p, int hz)
    {
        File.WriteAllBytes(bin, data);
        var header = $"""
                      ; Config for {src}
                      MAX_FRAMES   = {frames}
                      PLAYER_HZ    = {hz}
                      YM_DELAY     = {y}
                      YM_FINE      = {x}
                      PATTERN_SIZE = {p}
                      NUM_PATTERNS = {u}
                      SEQ_LEN      = {s}
                      """;

        File.WriteAllText(inc, header);
        Console.WriteLine(
            $"Wrote {data.Length} bytes. Ratio: {data.Length / (double)(frames * NumRegisters) * 100:F1}%");
    }

    /// <summary>
    /// Provides efficient O(N) sequence comparison for byte arrays to support pattern deduplication.
    /// </summary>
    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        /// <summary>
        /// Determines whether two byte arrays are equal using Span-based comparison.
        /// </summary>
        public bool Equals(byte[]? x, byte[]? y)
        {
            return x.AsSpan().SequenceEqual(y);
        }

        /// <summary>
        /// Generates a hash code for the byte array to allow its use in a Dictionary.
        /// </summary>
        public int GetHashCode(byte[] obj)
        {
            var h = new HashCode();
            h.AddBytes(obj);
            return h.ToHashCode();
        }
    }
}
