#!/usr/bin/env dotnet-script
using System.Diagnostics;
using System.Linq;

// YM to Atari 7800 Binary Converter (Pattern-Based)
// ------------------------------------------------
// Supports YM2, YM3, YM4, YM5, YM6 formats.
// Extracts repeating sequences to save space.

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

    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: dotnet script YmToBin.cs <input.ym> [output.bin] [max_frames] [pattern_size] [step] [-hz <value>]");
            return 1;
        }

        // Parse -hz <value> if it exists
        int? overrideHz = null;
        var argList = args.ToList();
        int hzIdx = argList.IndexOf("-hz");
        if (hzIdx != -1 && hzIdx + 1 < argList.Count)
        {
            overrideHz = int.Parse(argList[hzIdx + 1]);
            argList.RemoveAt(hzIdx + 1);
            argList.RemoveAt(hzIdx);
        }
        args = argList.ToArray();

        string inputFile = args[0];
        string outputFile = args.Length > 1 ? args[1] : Path.ChangeExtension(inputFile, ".bin");
        string configFile = Path.ChangeExtension(outputFile, ".yminc");
        int maxFramesInput = args.Length > 2 ? int.Parse(args[2]) : 65535;
        int patternSize = args.Length > 3 ? int.Parse(args[3]) : DefaultPatternSize;
        int step = args.Length > 4 ? int.Parse(args[4]) : 1;

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
            
            byte[] bestData = null;
            int bestSize = 0;
            int bestUnique = 0;
            int bestSeq = 0;

            if (args.Length > 3 && int.Parse(args[3]) > 0)
            {
                patternSize = int.Parse(args[3]);
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
            SaveOutput(outputFile, configFile, inputFile, bestData, header, outputFrames, yDelay, bestUnique, bestSeq, bestSize, effectiveHz);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

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
                int offset = header.DataOffset + (r * header.TotalFrames) + sourceFrame;
                frame[r] = rawData[offset];
            }

            int Scale(int low, int hi, int mask) {
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

        var compressedPatterns = new List<byte[]>();
        foreach (var raw in uniquePatternRaw)
        {
            compressedPatterns.Add(CompressPattern(raw));
        }

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

        return output.ToArray();
    }

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
                if (f == 0 || current != lastFrame[r])
                {
                    mask |= (ushort)(1 << r);
                    frameData.Add(current);
                }
                lastFrame[r] = current;
            }
            compressed.Add((byte)(mask & 0xFF));
            compressed.Add((byte)((mask >> 8) & 0xFF));
            compressed.AddRange(frameData);
        }
        return compressed.ToArray();
    }

    private static int CalculateDelay(int playerHz, out int fine)
    {
        // Atari 7800 NTSC PHI2 = 1.789773 MHz
        double ntscClock = 1789773.0; 
        double targetCycles = ntscClock / playerHz;
        
        // Estimated Player Overhead (fetching patterns, writing YM)
        double playerOverhead = 1800.0; 
        double remainingCycles = Math.Max(0, targetCycles - playerOverhead);
        
        // 1. Coarse Delay (Y Loop, 1285 cycles per step)
        int yDelay = (int)Math.Floor(remainingCycles / 1285.0);
        remainingCycles -= (yDelay * 1285.0);

        // 2. Fine Delay (X Loop, 5 cycles per step)
        fine = (int)Math.Round(remainingCycles / 5.0);
        if (fine > 255) fine = 255; 

        return (int)Math.Max(1, yDelay);
    }

    private static void SaveOutput(string binPath, string incPath, string inputFile, byte[] data, YMHeader header, int frames, int delay, int uniqueCount, int seqLen, int patternSize, int playerHz)
    {
        int yDelay = CalculateDelay(playerHz, out int xFine);

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
