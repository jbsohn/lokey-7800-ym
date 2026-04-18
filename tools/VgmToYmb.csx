#!/usr/bin/env dotnet-script
# nullable enable
#load "YmCommon.csx"
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

internal record VgmHeader(string Version, int DataOffset, int AyClock, int RateHz, string Title, string Author, string Game);

internal static class VgmConverter
{
    private const double Atari7800Clock = 1.792000;
    private const int VgmSampleRate = 44100;

    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help")) { PrintUsage(); return 1; }

        var options = ParseArgs(args);
        var binFile = options.InputFile;
        var outFile = options.OutputFile ?? Path.ChangeExtension(binFile, ".bin");
        var configFile = Path.ChangeExtension(outFile, ".ymi");

        try
        {
            var rawData = ExtractRawData(binFile);
            var header = ParseHeader(rawData);

            var playerHz = options.OverrideHz ?? header.RateHz;
            var effectiveHz = playerHz / options.Step;

            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine($"Song:   {header.Title}");
            Console.WriteLine($"Clock:  {header.AyClock / 1000000.0:F3} MHz | Rate: {effectiveHz} Hz");
            Console.WriteLine("---------------------------------------------------------");

            var music = ParseAndScaleVgm(rawData, header, playerHz, options.MaxFrames, options.Step);
            var bestData = music.Optimize(options.PatternSize, out var bestSize, out var u, out var s);

            var (y, x) = YmMusic.CalculateDelay(effectiveHz);
            YmMusic.Save(outFile, configFile, binFile, bestData, music.Frames.Count, y, x, u, s, bestSize, effectiveHz);
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); return 1; }
    }

    private static ConversionOptions ParseArgs(string[] args)
    {
        var input = args[0];
        string? output = null;
        int max = ushort.MaxValue, pat = 0, step = 1;
        int? hz = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (i + 1 >= args.Length) continue;
            switch (args[i])
            {
                case "-o": i++; output = args[i]; break;
                case "-f": i++; max = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-p": i++; pat = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-s": i++; step = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-hz": i++; hz = int.Parse(args[i], CultureInfo.InvariantCulture); break;
            }
        }
        return new ConversionOptions(input, output, max, pat, step, hz);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet script VgmToBin.cs <input.vgm/vgz> [options]");
        Console.WriteLine("Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'V' && buffer[1] == 'g' && buffer[2] == 'm') return buffer;

        var exeName = CommandLineUtils.IsToolInstalled("7z") ? "7z" : "7zz";
        using var process = Process.Start(new ProcessStartInfo(exeName, $"x -so \"{filePath}\"") { RedirectStandardOutput = true, UseShellExecute = false })
            ?? throw new InvalidOperationException("Failed to start extraction tool.");

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        return ms.ToArray();
    }

    private static VgmHeader ParseHeader(byte[] data)
    {
        if (Encoding.ASCII.GetString(data[..4]) != "Vgm ") throw new InvalidDataException("Not a VGM file.");
        var rateHz = BinaryPrimitives.ReadInt32LittleEndian(data[0x24..0x28]);
        if (rateHz == 0) rateHz = 60;
        var dataOffsetRel = BinaryPrimitives.ReadInt32LittleEndian(data[0x34..0x38]);
        var dataOffset = dataOffsetRel == 0 ? 0x40 : 0x34 + dataOffsetRel;
        var ayClock = BinaryPrimitives.ReadInt32LittleEndian(data[0x74..0x78]) & 0x3FFFFFFF;
        return new VgmHeader("1.71", dataOffset, ayClock, rateHz, "Unknown", "Unknown", "Unknown");
    }

    private static YmMusic ParseAndScaleVgm(byte[] rawData, VgmHeader header, int playerHz, int maxFrames, int step)
    {
        var pitchScale = Atari7800Clock / (header.AyClock / 1000000.0);
        var samplesPerFrame = (double)VgmSampleRate / playerHz;
        var registers = new byte[16];
        var workingRegs = new byte[16];
        var frames = new List<YmFrame>();
        var r13WasWritten = false;

        var offset = header.DataOffset;
        var currentSample = 0;
        double nextFrameSample = 0;

        while (offset < rawData.Length && frames.Count < maxFrames)
        {
            var cmd = rawData[offset++];
            if (cmd == 0xA0) { var reg = rawData[offset++]; var val = rawData[offset++]; if (reg < 16) { registers[reg] = val; if (reg == 13) r13WasWritten = true; } }
            else if (cmd == 0x61) { currentSample += BinaryPrimitives.ReadInt16LittleEndian(rawData[offset..(offset + 2)]); offset += 2; }
            else if (cmd == 0x62) currentSample += 735;
            else if (cmd == 0x63) currentSample += 882;
            else if ((cmd & 0xF0) == 0x70) currentSample += (cmd & 0x0F) + 1;
            else if (cmd == 0x66) break;
            else if (cmd == 0x67) { offset++; var size = BinaryPrimitives.ReadInt32LittleEndian(rawData[offset..(offset + 4)]); offset += 4 + size; }
            else if (cmd == 0x68) offset += 11;
            else
            {
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
                frames.Add(new YmFrame(registers, r13WasWritten).Scaled(pitchScale));
                r13WasWritten = false;
                nextFrameSample += samplesPerFrame;
            }
        }

        var steppedFrames = new List<YmFrame>();
        for (var i = 0; i < frames.Count; i += step)
        {
            Array.Clear(workingRegs, 0, 16);
            var r13InWindow = false;
            for (var s = 0; s < step && i + s < frames.Count; s++)
            {
                var f = frames[i + s];
                if (f.ForceEnvReset) r13InWindow = true;
                if (step > 1) { workingRegs[8] = Math.Max(workingRegs[8], f.VolumeA); workingRegs[9] = Math.Max(workingRegs[9], f.VolumeB); workingRegs[10] = Math.Max(workingRegs[10], f.VolumeC); if (s == 0) f.CopyTo(workingRegs, 16); }
                else f.CopyTo(workingRegs, 16);
            }
            steppedFrames.Add(new YmFrame(workingRegs, r13InWindow));
        }
        return new YmMusic(steppedFrames);
    }

    private static byte[] OptimizeCompression(YmMusic music, int manualPatSize, out int bestSize, out int u, out int s)
    {
        if (manualPatSize > 0)
        {
            bestSize = manualPatSize;
            return music.Compress(manualPatSize, out u, out s);
        }

        Console.WriteLine("Optimizing pattern size...");
        byte[]? bestData = null;
        bestSize = u = s = 0;
        int[] patternSizes = [16, 32, 48, 64, 80, 96, 128, 160, 192, 255];
        foreach (var size in patternSizes)
        {
            try
            {
                var trial = music.Compress(size, out var tu, out var ts);
                Console.WriteLine($"  Size {size,3}: {trial.Length,6} bytes ({tu,3} unique)");
                if (bestData == null || trial.Length < bestData.Length)
                {
                    (bestData, bestSize, u, s) = (trial, size, tu, ts);
                }
            }
            catch (InvalidOperationException) { }
        }
        return bestData ?? throw new InvalidOperationException("Optimization failed.");
    }

    private static int CalculateDelay(int hz, out int fine)
    {
        var (y, x) = YmMusic.CalculateDelay(hz);
        fine = (int)x;
        return y;
    }

    private static void SaveOutput(string bin, string inc, string src, byte[] data, int frames, int y, int x, int u, int s, int p, int hz)
    {
        YmMusic.Save(bin, inc, src, data, frames, y, x, u, s, p, hz);
    }
}
