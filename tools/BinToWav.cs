#!/usr/bin/env dotnet-script
# nullable enable
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

// BinToWav.cs - Reference-Grade YM2149 .bin to .wav Converter
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
            var incFile = Path.ChangeExtension(options.InputFile, ".yminc");
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
        int patSize = data[0], numPatterns = data[1], seqLen = data[2];
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
        var regs = new byte[14];

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

#pragma warning disable
// ReSharper disable All

/// <summary>
///     Literal C# port of aym-js by Olivier PONCET.
///     Synchronized with the js7800 emulator fork.
///     https://github.com/ponceto/aym-js
/// </summary>
internal class AymEmulator
{
    // YM2149 Logarithmic DAC Table
    private static readonly float[] YmDac =
    {
        0.0000000f, 0.0000000f, 0.0046540f, 0.0077211f, 0.0109560f, 0.0139620f, 0.0169986f, 0.0200198f,
        0.0243687f, 0.0296941f, 0.0350652f, 0.0403906f, 0.0485389f, 0.0583352f, 0.0680552f, 0.0777752f,
        0.0925154f, 0.1110857f, 0.1297475f, 0.1484855f, 0.1766690f, 0.2115511f, 0.2463874f, 0.2811017f,
        0.3337301f, 0.4004273f, 0.4673838f, 0.5344320f, 0.6351720f, 0.7580072f, 0.8799268f, 1.0000000f
    };

    private readonly EnvelopeGenerator _env = new();
    private readonly NoiseGenerator _noise = new();
    private readonly double _psgClock;
    private readonly byte[] _regs = new byte[16];
    private readonly double _sampleRate;

    private readonly ToneGenerator[] _tones = { new(), new(), new() };
    private double _lastVolume;
    private double _masterClock;
    private double _ticksAccumulator;

    public AymEmulator(double clk, double rate)
    {
        _masterClock = clk;
        _sampleRate = rate;
        _psgClock = clk / 8.0; // PSG internal units run at Master / 8
    }

    /// <summary>
    ///     Updates the internal register bank. R13 is treated as volatile to trigger envelope resets.
    /// </summary>
    public void UpdateRegisters(byte[] r)
    {
        for (var i = 0; i < 14; i++)
        {
            if (_regs[i] == r[i] && i != 13) continue;
            _regs[i] = r[i];
            if (i < 6) _tones[i / 2].Period = ((_regs[i / 2 * 2 + 1] & 0x0F) << 8) | _regs[i / 2 * 2];
            else if (i == 6) _noise.Period = _regs[6] & 0x1F;
            else if (i == 11 || i == 12) _env.Period = (_regs[12] << 8) | _regs[11];
            else if (i == 13) _env.Reset(_regs[13] & 0x0F);
        }
    }

    /// <summary>
    ///     Renders a single 44.1kHz audio sample. Uses box-filtering to average high-speed PSG ticks.
    /// </summary>
    public short RenderSample()
    {
        _ticksAccumulator += _psgClock / _sampleRate;
        var ticks = (int)_ticksAccumulator;
        _ticksAccumulator -= ticks;

        if (ticks > 0)
        {
            double sum = 0;
            for (var t = 0; t < ticks; t++)
            {
                foreach (var tone in _tones) tone.Clock();
                _noise.Clock();
                _env.Clock();

                double mixed = 0;
                for (var i = 0; i < 3; i++)
                {
                    // Mix logic: (ToneEnabled | TonePhase) & (NoiseEnabled | NoisePhase)
                    var toneHigh = (_regs[7] & (1 << i)) != 0 || _tones[i].Phase != 0;
                    var noiseHigh = (_regs[7] & (1 << (i + 3))) != 0 || _noise.Phase != 0;
                    if (toneHigh && noiseHigh)
                    {
                        var level = (_regs[8 + i] & 0x10) != 0 ? _env.Level : (_regs[8 + i] & 0x0F) * 2 + 1;
                        mixed += YmDac[Math.Clamp(level, 0, 31)];
                    }
                }

                sum += mixed / 3.0;
            }

            _lastVolume = sum / ticks;
        }

        // Apply a safe 40% gain and center the waveform
        var centered = (_lastVolume * 2.0 - 1.0) * 32767.0 * 0.4;
        return (short)Math.Clamp(centered, -32768, 32767);
    }

    private class ToneGenerator
    {
        public int Period, Counter, Phase = 1;

        public void Clock()
        {
            if (++Counter >= (Period == 0 ? 1 : Period))
            {
                Counter = 0;
                Phase ^= 1;
            }
        }
    }

    private class NoiseGenerator
    {
        public int Period, Counter, Phase = 1, Lfsr = 1;

        public void Clock()
        {
            if (++Counter >= (Period == 0 ? 1 : Period))
            {
                Counter = 0;
                // 17-bit XNOR LFSR implementation
                var bit0 = Lfsr & 1;
                var bit3 = (Lfsr >> 3) & 1;
                Lfsr = (Lfsr >> 1) | ((bit0 ^ bit3) << 16);
                Phase = Lfsr & 1;
            }
        }
    }

    private class EnvelopeGenerator
    {
        private bool _hold;
        public int Period, Counter, Level, Phase, Shape;

        public void Reset(int shape)
        {
            Shape = shape;
            Counter = 0;
            Phase = 0;
            _hold = false;
        }

        public void Clock()
        {
            if (_hold) return;
            if (++Counter >= (Period == 0 ? 1 : Period))
            {
                Counter = 0;
                // Complex cycle-based shape logic
                var attack = (Shape & 4) != 0;
                var alternate = (Shape & 2) != 0;
                var hold = (Shape & 1) != 0;
                var cont = (Shape & 8) != 0;

                if (Phase == 0)
                {
                    Level = attack ? Level + 1 : Level - 1;
                    if (Level < 0 || Level > 31)
                    {
                        if (!cont)
                        {
                            Level = 0;
                            _hold = true;
                        }
                        else if (hold)
                        {
                            Level = alternate ^ attack ? 0 : 31;
                            _hold = true;
                        }
                        else
                        {
                            if (alternate) Shape ^= 4;
                            Phase = 0;
                            Level = (Shape & 4) != 0 ? 0 : 31;
                        }
                    }
                }
            }
        }
    }
}