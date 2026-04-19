using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Core;

namespace YmToYmb;

/// <summary>
///     Represents the header and technical metadata of an Atari ST YM file.
/// </summary>
internal record YmHeader(string Signature, int TotalFrames, int ChipClock, int PlayerHz, int DataOffset);

internal static class Program
{
    private const double Atari7800Clock = 1.792000;

    public static int Main(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            PrintUsage();
            return 1;
        }

        var options = ParseArgs(args);
        var binFile = options.OutputFile ?? Path.ChangeExtension(options.InputFile, ".bin");
        var configFile = Path.ChangeExtension(binFile, ".ymi");

        try
        {
            var rawData = ExtractRawData(options.InputFile);
            var header = ParseHeader(rawData);

            var playerHz = options.OverrideHz ?? header.PlayerHz;
            var effectiveHz = playerHz / options.Step;

            PrintConversionSummary(options.InputFile, header, effectiveHz, options.Step);

            var music = DeinterleaveAndScale(rawData, header, options.MaxFrames, options.Step);
            var bestData = music.Optimize(options.PatternSize, out var bestSize, out var u, out var s);

            var (y, x) = YmMusic.CalculateDelay(effectiveHz);
            YmMusic.Save(binFile, configFile, options.InputFile, bestData, music.Frames.Count, y, x, u, s, bestSize,
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
    ///     Prints a summary of the YM file metadata and conversion settings.
    /// </summary>
    private static void PrintConversionSummary(string file, YmHeader header, int effectiveHz, int step)
    {
        Console.WriteLine("---------------------------------------------------------");
        Console.WriteLine($"Song:   {file}");
        Console.WriteLine($"Format: {header.Signature} | Rate: {effectiveHz} Hz (Step {step})");
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
        Console.WriteLine("Usage: YmToYmb <input.ym> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }

    /// <summary>
    ///     Reads the YM file into memory, handling lzh decompression if necessary via 7z.
    /// </summary>
    private static byte[] ExtractRawData(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        if (buffer.Length > 4 && buffer[0] == 'Y' && buffer[1] == 'M') return buffer;

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
    ///     Parses the YM file header to identify the format version and frame count.
    /// </summary>
    private static YmHeader ParseHeader(ReadOnlySpan<byte> data)
    {
        var sig = Encoding.ASCII.GetString(data[..4]);
        if (sig is "YM2!" or "YM3!") return ParseClassicHeader(sig, data);
        return ParseModernHeader(sig, data);
    }

    /// <summary>
    ///     Parses simple Atari ST YM2/YM3 headers.
    /// </summary>
    private static YmHeader ParseClassicHeader(string sig, ReadOnlySpan<byte> data)
    {
        return new YmHeader(sig, (data.Length - 4) / 14, 2000000, 50, 4);
    }

    /// <summary>
    ///     Parses modern Atari ST YM5/YM6 headers with extended metadata.
    /// </summary>
    private static YmHeader ParseModernHeader(string sig, ReadOnlySpan<byte> data)
    {
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
    ///     Deinterleaves the YM register data (which is stored per-register rather than per-frame) and scales pitch.
    /// </summary>
    private static YmMusic DeinterleaveAndScale(byte[] rawData, YmHeader header, int maxFrames, int step)
    {
        var outputFramesCount = (Math.Min(header.TotalFrames, maxFrames) + step - 1) / step;
        var frames = new List<YmFrame>(outputFramesCount);
        var pitchScale = Atari7800Clock / (header.ChipClock / 1000000.0);
        var registers = new byte[16];

        for (var f = 0; f < outputFramesCount; f++)
        {
            var r13Triggered = ProcessFrameWindow(rawData, header, f, step, registers);
            frames.Add(new YmFrame(registers, r13Triggered).Scaled(pitchScale));
            Array.Clear(registers, 0, 16);
        }

        return new YmMusic(frames);
    }

    /// <summary>
    ///     Processes a window of input frames to produce a single output frame, applying peak volume detection.
    /// </summary>
    private static bool ProcessFrameWindow(byte[] rawData, YmHeader header, int frameIdx, int step, byte[] registers)
    {
        var r13TriggeredInWindow = false;
        for (var s = 0; s < step; s++)
        {
            var sourceFrame = frameIdx * step + s;
            if (sourceFrame >= header.TotalFrames) break;

            for (var r = 0; r < 16; r++)
            {
                if (r >= 14 && header.Signature != "YM6!") break;
                var val = rawData[header.DataOffset + r * header.TotalFrames + sourceFrame];

                if (step > 1 && r is >= 8 and <= 10)
                    registers[r] = Math.Max(registers[r], val);
                else if (s == 0)
                    registers[r] = val;

                if (r != 13 || sourceFrame <= 0) continue;
                var prev = rawData[header.DataOffset + r * header.TotalFrames + sourceFrame - 1];
                if (val != prev || sourceFrame % 50 == 0) r13TriggeredInWindow = true;
            }
        }

        return r13TriggeredInWindow;
    }
}
