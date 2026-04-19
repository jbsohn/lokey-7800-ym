using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Core;

namespace VgmToYmb;

internal record VgmHeader(
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string Version,
    int DataOffset,
    int AyClock,
    int RateHz,
    string Title,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string Author,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    string Game);

internal static class Program
{
    private const double Atari7800Clock = 1.792000;
    private const int VgmSampleRate = 44100;

    public static int Main(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        var outFile = options.OutputFile ?? Path.ChangeExtension(options.InputFile, ".bin");
        var configFile = Path.ChangeExtension(outFile, ".ymi");

        try
        {
            var rawData = ExtractRawData(options.InputFile);
            var header = ParseHeader(rawData);

            var playerHz = options.OverrideHz ?? header.RateHz;
            var effectiveHz = playerHz / options.Step;

            PrintConversionSummary(header, effectiveHz);

            var music = ParseAndScaleVgm(rawData, header, playerHz, options.MaxFrames, options.Step);
            var bestData = music.Optimize(options.PatternSize, out var bestSize, out var u, out var s);

            var (y, x) = YmMusic.CalculateDelay(effectiveHz);
            YmMusic.Save(outFile, configFile, options.InputFile, bestData, music.Frames.Count, y, x, u, s, bestSize,
                effectiveHz);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    ///     Prints a summary of the song metadata and conversion parameters.
    /// </summary>
    private static void PrintConversionSummary(VgmHeader header, int effectiveHz)
    {
        Console.WriteLine("---------------------------------------------------------");
        Console.WriteLine($"Song:   {header.Title}");
        Console.WriteLine($"Clock:  {header.AyClock / 1000000.0:F3} MHz | Rate: {effectiveHz} Hz");
        Console.WriteLine("---------------------------------------------------------");
    }

    /// <summary>
    ///     Parses command-line arguments into a ConversionOptions record.
    /// </summary>
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
                case "-o":
                    i++;
                    output = args[i];
                    break;
                case "-f":
                    i++;
                    max = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "-p":
                    i++;
                    pat = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "-s":
                    i++;
                    step = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "-hz":
                    i++;
                    hz = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
            }
        }

        return new ConversionOptions(input, output, max, pat, step, hz);
    }

    /// <summary>
    ///     Displays command-line usage information.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: VgmToYmb <input.vgm/vgz> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

    /// <summary>
    ///     Reads the file into memory, handling gzip decompression if necessary.
    /// </summary>
    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'V' && buffer[1] == 'g' && buffer[2] == 'm') return buffer;

        var exeName = CommandLineUtils.IsToolInstalled("7z") ? "7z" : "7zz";
        using var process = Process.Start(new ProcessStartInfo(exeName, $"x -so \"{filePath}\"")
                                { RedirectStandardOutput = true, UseShellExecute = false })
                            ?? throw new InvalidOperationException("Failed to start extraction tool.");

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        process.WaitForExit();
        return ms.ToArray();
    }

    /// <summary>
    ///     Parses the VGM file header to extract clock rates and data offsets.
    /// </summary>
    private static VgmHeader ParseHeader(byte[] data)
    {
        if (Encoding.ASCII.GetString(data[..4]) != "Vgm ") throw new InvalidDataException("Not a VGM file.");

        var versionNum = BinaryPrimitives.ReadInt32LittleEndian(data[0x08..0x0C]);
        var version = $"{versionNum >> 8:X}.{versionNum & 0xFF:X2}";

        var rateHz = BinaryPrimitives.ReadInt32LittleEndian(data[0x24..0x28]);
        if (rateHz == 0) rateHz = 60;
        var dataOffsetRel = BinaryPrimitives.ReadInt32LittleEndian(data[0x34..0x38]);
        var dataOffset = dataOffsetRel == 0 ? 0x40 : 0x34 + dataOffsetRel;
        var ayClock = BinaryPrimitives.ReadInt32LittleEndian(data[0x74..0x78]) & 0x3FFFFFFF;
        return new VgmHeader(version, dataOffset, ayClock, rateHz, "Unknown", "Unknown", "Unknown");
    }

    /// <summary>
    ///     Orchestrates the conversion of raw VGM commands into a scaled YM music structure.
    /// </summary>
    private static YmMusic ParseAndScaleVgm(byte[] rawData, VgmHeader header, int playerHz, int maxFrames, int step)
    {
        var frames = ExtractVgmFrames(rawData, header, playerHz, maxFrames);
        var steppedFrames = ApplyFrameStepping(frames, step);
        return new YmMusic(steppedFrames);
    }

    /// <summary>
    ///     Iterates through the VGM command stream and samples PSG state at the target frame rate.
    /// </summary>
    private static List<YmFrame> ExtractVgmFrames(byte[] rawData, VgmHeader header, int playerHz, int maxFrames)
    {
        var pitchScale = Atari7800Clock / (header.AyClock / 1000000.0);
        var samplesPerFrame = (double)VgmSampleRate / playerHz;
        var registers = new byte[16];
        var frames = new List<YmFrame>();
        var r13WasWritten = false;

        var offset = header.DataOffset;
        var currentSample = 0;
        double nextFrameSample = 0;

        while (offset < rawData.Length && frames.Count < maxFrames)
        {
            if (!ProcessVgmCommand(rawData, ref offset, registers, ref currentSample, ref r13WasWritten))
                break;

            while (currentSample >= nextFrameSample && frames.Count < maxFrames)
            {
                frames.Add(new YmFrame(registers, r13WasWritten).Scaled(pitchScale));
                r13WasWritten = false;
                nextFrameSample += samplesPerFrame;
            }
        }

        return frames;
    }

    /// <summary>
    ///     Processes a single VGM command, updating PSG registers or advancing the sample clock.
    /// </summary>
    private static bool ProcessVgmCommand(byte[] data, ref int offset, byte[] registers, ref int currentSample,
        ref bool r13WasWritten)
    {
        var cmd = data[offset++];
        if (cmd == 0xA0)
        {
            var reg = data[offset++];
            var val = data[offset++];
            if (reg >= 16) return true;
            registers[reg] = val;
            if (reg == 13) r13WasWritten = true;
        }
        else if (cmd == 0x61)
        {
            currentSample += BinaryPrimitives.ReadInt16LittleEndian(data[offset..(offset + 2)]);
            offset += 2;
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
            return false;
        }
        else if (cmd == 0x67)
        {
            offset++;
            var size = BinaryPrimitives.ReadInt32LittleEndian(data[offset..(offset + 4)]);
            offset += 4 + size;
        }
        else if (cmd == 0x68)
        {
            offset += 11;
        }
        else
        {
            SkipUnsupportedVgmCommand(cmd, ref offset);
        }

        return true;
    }

    /// <summary>
    ///     Advances the data offset past VGM commands that are not relevant to YM2149 conversion.
    /// </summary>
    private static void SkipUnsupportedVgmCommand(byte cmd, ref int offset)
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

    /// <summary>
    ///     Downsamples the frame list using peak volume detection to preserve percussion and transients.
    /// </summary>
    private static List<YmFrame> ApplyFrameStepping(List<YmFrame> frames, int step)
    {
        var steppedFrames = new List<YmFrame>();
        var workingRegs = new byte[16];

        for (var i = 0; i < frames.Count; i += step)
        {
            Array.Clear(workingRegs, 0, 16);
            var r13InWindow = false;
            for (var s = 0; s < step && i + s < frames.Count; s++)
            {
                var f = frames[i + s];
                if (f.ForceEnvReset) r13InWindow = true;

                if (step > 1)
                {
                    UpdatePeakVolumes(workingRegs, f);
                    if (s == 0) f.CopyTo(workingRegs, 16);
                }
                else
                {
                    f.CopyTo(workingRegs, 16);
                }
            }

            steppedFrames.Add(new YmFrame(workingRegs, r13InWindow));
        }

        return steppedFrames;
    }

    /// <summary>
    ///     Updates the volume registers in the provided buffer to the maximum seen in the current window.
    /// </summary>
    private static void UpdatePeakVolumes(byte[] registers, YmFrame frame)
    {
        registers[8] = Math.Max(registers[8], frame.VolumeA);
        registers[9] = Math.Max(registers[9], frame.VolumeB);
        registers[10] = Math.Max(registers[10], frame.VolumeC);
    }
}
