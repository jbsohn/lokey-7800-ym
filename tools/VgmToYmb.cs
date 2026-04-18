#!/usr/bin/env dotnet-script
# nullable enable
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// VGM to Atari 7800 YMB Converter (Pattern-Based Delta)
// ------------------------------------------------------
// Optimized binary format for 6502 playback.
// Performs pitch scaling, delta-masking, and pattern deduplication.
// Features "Drum-Aware" peak detection for high-fidelity 30Hz conversions.
// ------------------------------------------------------

var arguments = Environment.GetCommandLineArgs().Skip(2).ToArray();
return VgmConverter.Run(arguments);

/// <summary>
///     Represents the metadata and structure of a VGM music file.
/// </summary>
internal record VgmHeader(
    string Version,
    int DataOffset,
    int AyClock,
    int RateHz,
    string Title,
    string Author,
    string Game
);

/// <summary>
///     Arguments passed to the conversion orchestrator.
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
///     Orchestrates the parsing, scaling, and compression of VGM/VGZ files.
/// </summary>
internal static class VgmConverter
{
    private const int NumRegisters = 14;
    private const double Atari7800Clock = 1.792000;
    private const int VgmSampleRate = 44100;

    /// <summary>
    ///     Main entry point for the VGM converter.
    /// </summary>
    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        var binFile = options.InputFile;
        var outFile = options.OutputFile ?? Path.ChangeExtension(binFile, ".bin");
        var configFile = Path.ChangeExtension(outFile, ".ymi");

        if (!IsToolInstalled("7z"))
            Console.WriteLine("WARNING: '7z' (7-Zip) not found. Required for compressed .vgz files.");

        try
        {
            var rawData = ExtractRawData(binFile);
            var header = ParseHeader(rawData);

            var playerHz = options.OverrideHz ?? header.RateHz;

            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine($"Song:   {header.Title}");
            Console.WriteLine($"Author: {header.Author}");
            Console.WriteLine($"Game:   {header.Game}");
            Console.WriteLine($"Format: VGM v{header.Version}");
            Console.WriteLine($"Clock:  {header.AyClock / 1000000.0:F3} MHz | Rate: {playerHz} Hz");
            Console.WriteLine("---------------------------------------------------------");

            var (interleavedData, r13Mask) =
                ParseAndScaleVgm(rawData, header, playerHz, options.MaxFrames, options.Step);
            var outputFrames = interleavedData.Length / NumRegisters;

            var (bestData, bestSize, bestUnique, bestSeq) =
                OptimizeCompression(interleavedData, outputFrames, options.PatternSize, r13Mask);

            var yDelay = CalculateDelay(playerHz / options.Step, out var xFine);
            SaveOutput(outFile, configFile, binFile, bestData, outputFrames, yDelay, xFine, bestUnique, bestSeq,
                bestSize, playerHz / options.Step);
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
        Console.WriteLine("Usage: dotnet script VgmToBin.cs <input.vgm/vgz> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

    private static bool IsToolInstalled(string toolName)
    {
        try
        {
            var searchCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            using var p = Process.Start(new ProcessStartInfo(searchCmd, toolName)
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
    ///     Extracts raw VGM data, handling transparent decompression of .vgz files.
    /// </summary>
    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'V' && buffer[1] == 'g' && buffer[2] == 'm') return buffer;

        var exeName = IsToolInstalled("7z") ? "7z" : "7zz";

        using var process = Process.Start(new ProcessStartInfo(exeName, $"x -so \"{filePath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Failed to start '{exeName}'.");

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        var outData = ms.ToArray();
        if (outData.Length > 4 && outData[0] == 'V' && outData[1] == 'g' && outData[2] == 'm') return outData;

        throw new InvalidDataException("File is not a valid VGM or compressed VGZ.");
    }

    /// <summary>
    ///     Parses the VGM header and extracts metadata like chip clocks and GD3 tags.
    /// </summary>
    private static VgmHeader ParseHeader(byte[] data)
    {
        if (Encoding.ASCII.GetString(data[..4]) != "Vgm ")
            throw new InvalidDataException("Not a VGM file.");

        var versionNum = BinaryPrimitives.ReadInt32LittleEndian(data[0x08..0x0C]);
        var version = $"{versionNum >> 8:X}.{versionNum & 0xFF:X2}";

        var rateHz = BinaryPrimitives.ReadInt32LittleEndian(data[0x24..0x28]);
        if (rateHz == 0) rateHz = 60;

        var dataOffsetRel = BinaryPrimitives.ReadInt32LittleEndian(data[0x34..0x38]);
        var dataOffset = dataOffsetRel == 0 ? 0x40 : 0x34 + dataOffsetRel;

        var ayClock = BinaryPrimitives.ReadInt32LittleEndian(data[0x74..0x78]);
        if (ayClock == 0)
            throw new InvalidDataException("VGM does not contain AY8910/YM2149 data.");

        ayClock &= 0x3FFFFFFF;
        var (title, author, game) = ExtractMetadata(data);
        return new VgmHeader(version, dataOffset, ayClock, rateHz, title, author, game);
    }

    private static (string title, string author, string game) ExtractMetadata(byte[] data)
    {
        var gd3OffsetRel = BinaryPrimitives.ReadInt32LittleEndian(data[0x14..0x18]);
        if (gd3OffsetRel == 0) return ("Unknown", "Unknown", "Unknown");

        var offset = 0x14 + gd3OffsetRel;
        if (Encoding.ASCII.GetString(data[offset..(offset + 4)]) != "Gd3 ")
            return ("Unknown", "Unknown", "Unknown");

        offset += 12;

        string ReadString(ref int off)
        {
            var start = off;
            while (off + 1 < data.Length && (data[off] != 0 || data[off + 1] != 0)) off += 2;
            var res = Encoding.Unicode.GetString(data[start..off]);
            off += 2;
            return res;
        }

        var title = ReadString(ref offset);
        _ = ReadString(ref offset);
        var game = ReadString(ref offset);
        _ = ReadString(ref offset);
        _ = ReadString(ref offset);
        _ = ReadString(ref offset);
        var author = ReadString(ref offset);

        return (title, author, game);
    }

    /// <summary>
    ///     Parses the VGM command stream, scales pitches, and implements Peak Detection for frame stepping.
    /// </summary>
    private static (byte[] data, bool[] r13Written) ParseAndScaleVgm(byte[] rawData, VgmHeader header, int playerHz,
        int maxFrames, int step)
    {
        var pitchScale = Atari7800Clock / (header.AyClock / 1000000.0);
        var samplesPerFrame = (double)VgmSampleRate / playerHz;

        var registers = new byte[NumRegisters];
        var workingRegs = new byte[NumRegisters];
        var frames = new List<byte[]>();
        var r13Writes = new List<bool>();
        var r13WasWritten = false;

        var offset = header.DataOffset;
        var currentSample = 0;
        double nextFrameSample = 0;

        while (offset < rawData.Length && frames.Count < maxFrames)
        {
            var cmd = rawData[offset++];

            if (cmd == 0xA0) // AY8910 write
            {
                var reg = rawData[offset++];
                var val = rawData[offset++];
                if (reg < NumRegisters)
                {
                    registers[reg] = val;
                    if (reg == 13) r13WasWritten = true;
                }
            }
            else if (cmd == 0x61)
            {
                int wait = BinaryPrimitives.ReadInt16LittleEndian(rawData[offset..(offset + 2)]);
                offset += 2;
                currentSample += wait;
            }
            else if (cmd == 0x62)
            {
                currentSample += 735;
            }
            else if (cmd == 0x63)
            {
                currentSample += 882;
            }
            else if ((cmd & 0xF0) == 0x70)
            {
                currentSample += (cmd & 0x0F) + 1;
            }
            else if (cmd == 0x66)
            {
                break;
            }
            else if (cmd == 0x67) // Data Block (Metadata/PCM)
            {
                offset++; // data type
                var size = BinaryPrimitives.ReadInt32LittleEndian(rawData[offset..(offset + 4)]);
                offset += 4 + size;
            }
            else if (cmd == 0x68)
            {
                offset += 11;
            }
            else
            {
                // General command skipping to maintain stream alignment
                switch (cmd)
                {
                    case >= 0x30 and <= 0x3F: break;
                    case >= 0x40 and <= 0x4F: offset++; break;
                    case >= 0x50 and <= 0x5F:
                    case >= 0xA0 and <= 0xBF: offset += 2; break;
                    case >= 0xC0 and <= 0xDF: offset += 3; break;
                }
            }

            while (currentSample >= nextFrameSample && frames.Count < maxFrames)
            {
                // Capture current state for this 1/60th (or 1/50th) second frame
                frames.Add(ScaleFrame(registers, pitchScale));
                r13Writes.Add(r13WasWritten);
                r13WasWritten = false;
                nextFrameSample += samplesPerFrame;
            }
        }

        var steppedFrames = new List<byte>();
        var steppedR13 = new List<bool>();

        // Implementation of Drum-Aware Peak Detection
        for (var i = 0; i < frames.Count; i += step)
        {
            Array.Clear(workingRegs, 0, NumRegisters);
            var r13TriggeredInWindow = false;

            // Scan the window (e.g. 2 frames) to protect percussion peaks
            for (var s = 0; s < step && i + s < frames.Count; s++)
            {
                var frameData = frames[i + s];
                if (r13Writes[i + s]) r13TriggeredInWindow = true;

                for (var r = 0; r < NumRegisters; r++)
                    // Peak detect volume (R8,9,10) to prevent missing drum attacks
                    if (step > 1 && r is >= 8 and <= 10)
                    {
                        if (frameData[r] > workingRegs[r]) workingRegs[r] = frameData[r];
                    }
                    // Capture first frame for pitch/period to maintain tuning
                    else if (s == 0)
                    {
                        workingRegs[r] = frameData[r];
                    }
            }

            steppedFrames.AddRange(workingRegs);
            steppedR13.Add(r13TriggeredInWindow);
        }

        return (steppedFrames.ToArray(), steppedR13.ToArray());
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
        frame[6] = (byte)((int)Math.Round((frame[6] & 0x1F) * pitchScale) & 0x1F);
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

        if (bestData == null)
            throw new InvalidOperationException("Could not find a pattern size that fits within 8-bit limits.");
        return (bestData, bestSize, bestU, bestS);
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