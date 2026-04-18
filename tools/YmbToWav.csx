#!/usr/bin/env dotnet-script
# nullable enable
#load "YmCommon.csx"
#load "AymEmulator.cs"
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

// YmbToWav.cs - Reference-Grade YM2149 .ymb to .wav Converter
// -------------------------------------------------------------
// Uses a literal C# port of aym-js by Olivier PONCET.
// https://github.com/ponceto/aym-js
// Synchronized with the js7800 emulator fork.
// -------------------------------------------------------------

var arguments = Environment.GetCommandLineArgs().Skip(2).ToArray();
return BinToWavConverter.Run(arguments);

/// <summary>
///     Options for the WAV conversion process.
/// </summary>
internal record struct WavOptions(string InputFile, string OutputFile);

/// <summary>
///     Orchestrates the conversion of compressed 7800 music binaries into high-accuracy WAV files.
/// </summary>
internal static class BinToWavConverter
{
    /// <summary>
    ///     Entry point for the conversion tool.
    /// </summary>
    public static int Run(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        if (!File.Exists(options.InputFile))
        {
            Console.Error.WriteLine($"Error: File not found: {options.InputFile}");
            return 1;
        }

        try
        {
            var binData = File.ReadAllBytes(options.InputFile);
            var incFile = Path.ChangeExtension(options.InputFile, ".ymi");
            var playerHz = DetectPlayerHz(incFile);

            var renderer = new BinToWavRenderer(binData, playerHz);
            renderer.SaveToWav(options.OutputFile);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static WavOptions ParseArgs(string[] args)
    {
        return new WavOptions(args[0], args.Length > 1 ? args[1] : Path.ChangeExtension(args[0], ".wav"));
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet script BinToWav.cs <input.bin> [output.wav]");
    }

    /// <summary>
    ///     Extracts the PLAYER_HZ metadata from the sidecar .yminc file to ensure correct timing.
    /// </summary>
    private static int DetectPlayerHz(string incFile)
    {
        if (!File.Exists(incFile)) return 60;
        var hzMatch = Regex.Match(File.ReadAllText(incFile), @"PLAYER_HZ\s*=\s*(\d+)");
        return hzMatch.Success ? int.Parse(hzMatch.Groups[1].Value) : 60;
    }
}

/// <summary>
///     Handles the high-fidelity rendering process by driving the AymEmulator.
/// </summary>
internal class BinToWavRenderer(byte[] data, int playerHz)
{
    private const int SampleRate = 44100;
    private const double ClockSpeed = 1792000.0; // Atari 7800 PHI2 Clock
    private readonly AymEmulator _emu = new(ClockSpeed, SampleRate);

    /// <summary>
    ///     Renders the entire binary track into a standard 16-bit PCM Mono WAV file.
    /// </summary>
    public void SaveToWav(string filePath)
    {
        int patSize = data[0] == 0 ? 256 : data[0];
        int numPatterns = data[1], seqLen = data[2];
        var samplesPerFrame = SampleRate / playerHz;
        var totalSamples = (long)seqLen * patSize * samplesPerFrame;

        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        // RIFF/WAVE Header
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write((uint)(36 + totalSamples * 2));
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write((uint)16);
        bw.Write((ushort)1); // Mono
        bw.Write((ushort)1); // PCM
        bw.Write((uint)SampleRate);
        bw.Write((uint)(SampleRate * 2));
        bw.Write((ushort)2); // 16-bit
        bw.Write((ushort)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write((uint)(totalSamples * 2));

        var sequence = data.AsSpan(3, seqLen);
        var offsetTableStart = 3 + seqLen;
        var offsets = new ushort[numPatterns];
        for (var i = 0; i < numPatterns; i++)
            offsets[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offsetTableStart + i * 2));

        var patDataStart = offsetTableStart + numPatterns * 2;
        var regs = new byte[16];

        Console.Error.WriteLine($"js7800 Rendering: {seqLen} patterns...");

        foreach (var patId in sequence)
        {
            var musicPtr = patDataStart + offsets[patId];
            for (var f = 0; f < patSize; f++)
            {
                if (musicPtr + 2 > data.Length) break;
                var mask = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(musicPtr));
                musicPtr += 2;
                for (var r = 0; r < 14; r++)
                    if ((mask & (1 << r)) != 0)
                        regs[r] = data[musicPtr++];

                _emu.UpdateRegisters(regs);
                for (var s = 0; s < samplesPerFrame; s++)
                    bw.Write(_emu.RenderSample());
            }
        }

        Console.Error.WriteLine($"Success! Created {filePath}");
    }
}
