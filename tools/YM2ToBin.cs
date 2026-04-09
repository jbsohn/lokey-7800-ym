#!/usr/bin/env dotnet-script
using System.Diagnostics;

// YM to Atari 7800 Binary Converter
// ----------------------------------------
// Supports YM2, YM3, YM4, YM5, YM6 formats.

#pragma warning disable CA1050 // Script code does not support namespaces

var commandLineArgs = Environment.GetCommandLineArgs().Skip(2).ToArray();
return YMConverter.Run(commandLineArgs);

/// <summary>
/// Represents the metadata header extracted from a YM file.
/// </summary>
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
    private const int CyclesPerYCycle = 1280; // 5 cycles/dex * 256

    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet script YM2ToBin.cs <input.ym> <output.bin> [max_frames]");
            return 1;
        }

        string inputFile = args[0];
        string outputFile = args[1];
        string configFile = Path.ChangeExtension(outputFile, ".inc");
        int maxFramesInput = args.Length > 2 ? int.Parse(args[2]) : 3000;

        try
        {
            byte[] rawData = ExtractRawData(inputFile);
            YMHeader header = ParseHeader(rawData);
            int framesToProcess = Math.Min(header.TotalFrames, maxFramesInput);

            Console.WriteLine($"Processing {inputFile} -> {outputFile}");
            Console.WriteLine($"Format: {header.Signature} | Total Frames: {header.TotalFrames} | Scaling for {header.ChipClock}Hz to {Atari7800Clock}MHz");

            byte[] interleavedData = DeinterleaveAndScale(rawData, header, framesToProcess);
            var (data, activity) = Compress(interleavedData, framesToProcess);
            int yDelay = CalculateDelay(header.PlayerHz);

            SaveOutput(outputFile, configFile, inputFile, data, header, framesToProcess, yDelay, activity);
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
            for (int i = 0; i < 3; i++)
            {
                while (data[skip] != 0) skip++;
                skip++;
            }
            header.DataOffset = skip;
        }
        else
        {
            throw new Exception($"Unsupported format '{signature}'");
        }
        return header;
    }

    private static byte[] DeinterleaveAndScale(byte[] rawData, YMHeader header, int framesToProcess)
    {
        byte[] interleavedData = new byte[framesToProcess * NumRegisters];
        double pitchScale = Atari7800Clock / (header.ChipClock / 1000000.0);

        for (int f = 0; f < framesToProcess; f++)
        {
            byte[] frame = new byte[NumRegisters];
            for (int r = 0; r < NumRegisters; r++)
            {
                frame[r] = rawData[header.DataOffset + (r * header.TotalFrames) + f];
            }

            // Apply Pitch Scaling (Tone A, B, C, Noise, Env)
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
            {
                interleavedData[(f * NumRegisters) + r] = frame[r];
            }
        }
        return interleavedData;
    }

    private static (byte[] Data, int[] Activity) Compress(byte[] interleavedData, int framesToProcess)
    {
        List<byte> compressedData = new List<byte>();
        byte[] lastFrame = new byte[NumRegisters];
        int[] perRegChanges = new int[NumRegisters];

        for (int f = 0; f < framesToProcess; f++)
        {
            ushort mask = 0;
            List<byte> frameData = new List<byte>();
            for (int r = 0; r < NumRegisters; r++)
            {
                byte current = interleavedData[(f * NumRegisters) + r];
                if (f == 0 || current != lastFrame[r])
                {
                    mask |= (ushort)(1 << r);
                    frameData.Add(current);
                    perRegChanges[r]++;
                }
                lastFrame[r] = current;
            }
            compressedData.Add((byte)(mask & 0xFF));
            compressedData.Add((byte)((mask >> 8) & 0xFF));
            compressedData.AddRange(frameData);
        }
        return (compressedData.ToArray(), perRegChanges);
    }

    private static int CalculateDelay(int playerHz)
    {
        double cyclesPerFrame = (Atari7800Clock * 1000000.0) / playerHz;
        return (int)Math.Round(cyclesPerFrame / CyclesPerYCycle);
    }

    private static void SaveOutput(string binPath, string incPath, string inputFile, byte[] data, YMHeader header, int frames, int delay, int[] activity)
    {
        File.WriteAllBytes(binPath, data);

        using (var sw = new StreamWriter(incPath))
        {
            sw.WriteLine($"; Generated config for {inputFile}");
            sw.WriteLine($"MAX_FRAMES = {frames}");
            sw.WriteLine($"YM_DELAY   = ${delay:X2}");
            sw.WriteLine($"YM_CLOCK   = {header.ChipClock}");
            sw.WriteLine($"YM_HZ      = {header.PlayerHz}");
        }

        Console.WriteLine($"Wrote {data.Length} bytes to {binPath} (Compressed)");
        Console.WriteLine($"Compression Ratio: {(double)data.Length / (frames * NumRegisters) * 100:F1}%");
        Console.WriteLine($"Generated {incPath} with Delay=${delay:X2}");

        Console.WriteLine("\n--- Register Activity ---");
        for (int r = 0; r < NumRegisters; r++)
        {
            Console.WriteLine($"Reg {r:D2}: {activity[r],4} writes ({(double)activity[r] / frames * 100,5:F1}%)");
        }
    }
}
