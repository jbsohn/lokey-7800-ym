#!/usr/bin/env dotnet-script
# nullable enable
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;

// YM to Atari 7800 YMB Converter (Pattern-Based Delta)
// ------------------------------------------------------
// Optimized binary format for 6502 playback.
// Performs pitch scaling, delta-masking, and pattern deduplication.
// Features "Drum-Aware" peak detection for high-fidelity 30Hz conversions.
// ------------------------------------------------------

var arguments = Environment.GetCommandLineArgs().Skip(2).ToArray();
return YmConverter.Run(arguments);

/// <summary>
///     Represents the header and technical metadata of an Atari ST YM file.
/// </summary>
internal record YmHeader(
    string Signature,
    int TotalFrames,
    int ChipClock,
    int PlayerHz,
    int DataOffset
);

/// <summary>
///     Arguments passed to the YM conversion orchestrator.
/// </summary>
internal record struct ConversionOptions(
    string InputFile,
    string? OutputFile,
    int MaxFrames,
    int PatternSize,
    int Step,
    int? OverrideHz
);

/// <summary>
///     Orchestrates the extraction, scaling, and compression of Atari ST YM assets.
/// </summary>
internal static class YmConverter
{
    private const int NumRegisters = 14;
    private const double Atari7800Clock = 1.792000;

    /// <summary>
    ///     Main entry point for the YM converter.
    /// </summary>
    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        var binFile = options.OutputFile ?? Path.ChangeExtension(options.InputFile, ".bin");
        var configFile = Path.ChangeExtension(binFile, ".ymi");

        if (!IsToolInstalled("7z"))
            Console.WriteLine("WARNING: '7z' (7-Zip) not found. Required for compressed .ym files.");

        try
        {
            var rawData = ExtractRawData(options.InputFile);
            var header = ParseHeader(rawData);

            var playerHz = options.OverrideHz ?? header.PlayerHz;
            var framesToProcess = Math.Min(header.TotalFrames, options.MaxFrames);
            var effectiveHz = playerHz / options.Step;

            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine($"Song:   {options.InputFile}");
            Console.WriteLine($"Format: {header.Signature} | Frames: {header.TotalFrames}");
            Console.WriteLine(
                $"Clock:  {header.ChipClock / 1000000.0:F3} MHz | Rate: {effectiveHz} Hz (Step {options.Step})");
            Console.WriteLine("---------------------------------------------------------");

            var (interleavedData, r13Mask) = DeinterleaveAndScale(rawData, header, framesToProcess, options.Step);
            var outputFrames = interleavedData.Length / NumRegisters;

            var (bestData, bestSize, bestUnique, bestSeq) =
                OptimizeCompression(interleavedData, outputFrames, options.PatternSize, r13Mask);

            var yDelay = CalculateDelay(effectiveHz, out var xFine);
            SaveOutput(binFile, configFile, options.InputFile, bestData, outputFrames, yDelay, xFine, bestUnique,
                bestSeq,
                bestSize, effectiveHz);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet script YmToBin.cs <input.ym> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

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
    ///     Uses 7-Zip to extract raw YM data from LZH-compressed containers.
    /// </summary>
    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'Y' && buffer[1] == 'M') return buffer;

        using var process = Process.Start(new ProcessStartInfo("7z", $"x -so \"{filePath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start '7z'. Ensure 7-Zip is installed.");

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        return ms.ToArray();
    }

    /// <summary>
    ///     Parses the YM header, identifying the format version and jumping over digidrum blocks.
    /// </summary>
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
    ///     Implements Peak Detection during frame-stepping to preserve percussive attacks.
    /// </summary>
    private static (byte[] data, bool[] r13Written) DeinterleaveAndScale(byte[] rawData, YmHeader header,
        int framesToProcess, int step)
    {
        var outputFrames = (framesToProcess + step - 1) / step;
        var interleavedData = new byte[outputFrames * NumRegisters];
        var r13Writes = new bool[outputFrames];
        var pitchScale = Atari7800Clock / (header.ChipClock / 1000000.0);

        var registers = new byte[NumRegisters];

        for (var f = 0; f < outputFrames; f++)
        {
            var r13TriggeredInWindow = false;

            // Peak Detection: Scan all frames in the step window
            for (var s = 0; s < step; s++)
            {
                var sourceFrame = f * step + s;
                if (sourceFrame >= header.TotalFrames) break;

                for (var r = 0; r < NumRegisters; r++)
                {
                    var val = rawData[header.DataOffset + r * header.TotalFrames + sourceFrame];

                    // Take the MAX volume for drum channels (R8,9,10) during stepping
                    if (step > 1 && r is >= 8 and <= 10)
                    {
                        if (val > registers[r]) registers[r] = val;
                    }
                    // Capture first value in window for pitch to avoid tuning issues
                    else if (s == 0)
                    {
                        registers[r] = val;
                    }

                    // Special R13 logic: detect change or trigger across skipping window
                    if (r == 13 && sourceFrame > 0)
                    {
                        var prev = rawData[header.DataOffset + r * header.TotalFrames + sourceFrame - 1];
                        if (val != prev || sourceFrame % 50 == 0) r13TriggeredInWindow = true;
                    }
                }
            }

            var scaledFrame = ScaleFrame(registers, pitchScale);
            scaledFrame.CopyTo(interleavedData.AsSpan(f * NumRegisters));
            r13Writes[f] = r13TriggeredInWindow;

            Array.Clear(registers, 0, registers.Length);
        }

        return (interleavedData, r13Writes);
    }

    /// <summary>
    ///     Re-calculates frequency and period registers to match the Atari 7800 PHI2 clock.
    /// </summary>
    private static byte[] ScaleFrame(byte[] registers, double pitchScale)
    {
        var frame = new byte[NumRegisters];
        Array.Copy(registers, frame, NumRegisters);
        var tA = Scale(frame[0], frame[1], 0x0F, pitchScale);
        frame[0] = (byte)(tA & 0xFF);
        frame[1] = (byte)((tA >> 8) & 0x0F);
        var tB = Scale(frame[2], frame[3], 0x0F, pitchScale);
        frame[2] = (byte)(tB & 0xFF);
        frame[3] = (byte)((tB >> 8) & 0x0F);
        var tC = Scale(frame[4], frame[5], 0x0F, pitchScale);
        frame[4] = (byte)(tC & 0xFF);
        frame[5] = (byte)((tC >> 8) & 0x0F);
        var frame6Raw = frame[6] & 0x1F;
        frame[6] = (byte)((int)Math.Round(frame6Raw * pitchScale) & 0x1F);
        var e = Scale(frame[11], frame[12], 0xFF, pitchScale);
        frame[11] = (byte)(e & 0xFF);
        frame[12] = (byte)((e >> 8) & 0xFF);
        return frame;

        static int Scale(int low, int hi, int mask, double scale)
        {
            return (int)Math.Round((((hi & mask) << 8) | low) * scale);
        }
    }

    private static (byte[] data, int size, int unique, int seq) OptimizeCompression(byte[] interleavedData, int frames,
        int manualPatSize, bool[] r13Mask)
    {
        if (manualPatSize > 0)
        {
            var data = CompressWithPatterns(interleavedData, frames, manualPatSize, out var u, out var s, r13Mask);
            return (data, manualPatSize, u, s);
        }

        Console.WriteLine("Optimizing pattern size...");
        byte[]? bestData = null;
        int bestSize = 0, bestU = 0, bestS = 0;
        int[] patternSizes = [16, 32, 48, 64, 80, 96, 128, 160, 192, 255];
        foreach (var size in patternSizes)
            try
            {
                var trial = CompressWithPatterns(interleavedData, frames, size, out var u, out var s, r13Mask);
                Console.WriteLine($"  Size {size,3}: {trial.Length,6} bytes ({u,3} unique)");
                if (bestData == null || trial.Length < bestData.Length)
                    (bestData, bestSize, bestU, bestS) = (trial, size, u, s);
            }
            catch (InvalidOperationException)
            {
            }

        return bestData == null
            ? throw new InvalidOperationException("Could not find a pattern size that fits within 8-bit limits.")
            : (bestData, bestSize, bestU, bestS);
    }

    private static byte[] CompressWithPatterns(byte[] interleavedData, int totalFrames, int patSize,
        out int uniqueCount, out int seqLen, bool[] r13Mask)
    {
        var numBlocks = (int)Math.Ceiling((double)totalFrames / patSize);
        if (numBlocks > 255) throw new InvalidOperationException($"Song too long! ({numBlocks} blocks)");

        var uniquePatterns = new List<(byte[] data, bool[] r13)>();
        var sequence = new List<int>();
        var lookup = new Dictionary<(byte[] data, bool[] r13), int>(new PatternComparer());

        for (var b = 0; b < numBlocks; b++)
        {
            var framesInBlock = Math.Min(patSize, totalFrames - b * patSize);
            var blockData = new byte[framesInBlock * NumRegisters];
            var blockR13 = new bool[framesInBlock];
            Array.Copy(interleavedData, b * patSize * NumRegisters, blockData, 0, blockData.Length);
            Array.Copy(r13Mask, b * patSize, blockR13, 0, blockR13.Length);

            var key = (blockData, blockR13);
            if (!lookup.TryGetValue(key, out var id))
            {
                id = uniquePatterns.Count;
                if (id > 255) throw new InvalidOperationException("Too many unique patterns.");
                uniquePatterns.Add(key);
                lookup[key] = id;
            }

            sequence.Add(id);
        }

        uniqueCount = uniquePatterns.Count;
        seqLen = sequence.Count;

        List<byte> output = [(byte)patSize, (byte)uniqueCount, (byte)seqLen];
        output.AddRange(sequence.Select(id => (byte)id));

        var compressed = uniquePatterns.Select(p => CompressPattern(p.data, p.r13)).ToList();
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
    ///     Performs frame-to-frame delta masking. R13 is treated as volatile to preserve envelope resets.
    /// </summary>
    private static byte[] CompressPattern(byte[] raw, bool[] r13Written)
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
                var forceWrite = r == 13 && r13Written[f];
                if (f == 0 || current != last[r] || forceWrite)
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

    private static int CalculateDelay(int hz, out int fine)
    {
        var remaining = Math.Max(0, 1789773.0 / hz - 1800.0);
        var y = (int)Math.Floor(remaining / 1285.0);
        fine = (int)Math.Min(255, Math.Round((remaining - y * 1285.0) / 5.0));
        return Math.Max(1, y);
    }

    private static void SaveOutput(string bin, string inc, string src, byte[] data, int frames, int y, int x, int u,
        int s, int p, int hz)
    {
        File.WriteAllBytes(bin, data);
        File.WriteAllText(inc,
            $"; Config for {src}\nMAX_FRAMES   = {frames}\nPLAYER_HZ    = {hz}\nYM_DELAY     = {y}\nYM_FINE      = {x}\nPATTERN_SIZE = {p}\nNUM_PATTERNS = {u}\nSEQ_LEN      = {s}\n");
        Console.WriteLine(
            $"Wrote {data.Length} bytes. Ratio: {data.Length / (double)(frames * NumRegisters) * 100:F1}%");
    }

    private sealed class PatternComparer : IEqualityComparer<(byte[] data, bool[] r13)>
    {
        public bool Equals((byte[] data, bool[] r13) x, (byte[] data, bool[] r13) y)
        {
            return x.data.AsSpan().SequenceEqual(y.data) && x.r13.AsSpan().SequenceEqual(y.r13);
        }

        public int GetHashCode((byte[] data, bool[] r13) obj)
        {
            var h = new HashCode();
            h.AddBytes(obj.data);
            foreach (var b in obj.r13) h.Add(b);
            return h.ToHashCode();
        }
    }
}