# nullable enable
#load "AymEmulator.cs"
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Buffers.Binary;

/// <summary>
///     Arguments passed to the conversion orchestrators.
/// </summary>
internal record struct ConversionOptions(
    string InputFile, string? OutputFile,
    int MaxFrames, int PatternSize,
    int Step, int? OverrideHz
);

/// <summary>
///     Represents a single audio frame of YM2149 / YM6 register data.
/// </summary>
internal record struct YmFrame(
    byte PeriodLowA, byte PeriodHighA,
    byte PeriodLowB, byte PeriodHighB,
    byte PeriodLowC, byte PeriodHighC,
    byte NoisePeriod, byte Mixer,
    byte VolumeA, byte VolumeB, byte VolumeC,
    byte EnvPeriodLow, byte EnvPeriodHigh, byte EnvShape,
    byte EffectType = 0, byte EffectData = 0,
    bool ForceEnvReset = false
)
{
    /// <summary>
    ///     Constructs a YmFrame from a raw buffer of register values.
    /// </summary>
    public YmFrame(ReadOnlySpan<byte> data, bool forceReset = false) : this(
        data[0], data[1], data[2], data[3], data[4], data[5], data[6],
        data[7], data[8], data[9], data[10], data[11], data[12], data[13],
        data.Length > 14 ? data[14] : (byte)0,
        data.Length > 15 ? data[15] : (byte)0,
        forceReset
    ) { }

    // Hardware Property Mappings
    public ushort ToneA => (ushort)(((PeriodHighA & 0x0F) << 8) | PeriodLowA);
    public ushort ToneB => (ushort)(((PeriodHighB & 0x0F) << 8) | PeriodLowB);
    public ushort ToneC => (ushort)(((PeriodHighC & 0x0F) << 8) | PeriodLowC);
    public ushort EnvPeriod => (ushort)((EnvPeriodHigh << 8) | EnvPeriodLow);

    /// <summary>
    ///     Scales the frequency-related registers by a ratio to compensate for differences in hardware clock speed.
    /// </summary>
    public YmFrame Scaled(double ratio)
    {
        var tA = (ushort)Math.Round(ToneA * ratio);
        var tB = (ushort)Math.Round(ToneB * ratio);
        var tC = (ushort)Math.Round(ToneC * ratio);
        var e = (ushort)Math.Round(EnvPeriod * ratio);
        var n = (byte)Math.Round((NoisePeriod & 0x1F) * ratio);

        return this with {
            PeriodLowA = (byte)(tA & 0xFF), PeriodHighA = (byte)((tA >> 8) & 0x0F),
            PeriodLowB = (byte)(tB & 0xFF), PeriodHighB = (byte)((tB >> 8) & 0x0F),
            PeriodLowC = (byte)(tC & 0xFF), PeriodHighC = (byte)((tC >> 8) & 0x0F),
            EnvPeriodLow = (byte)(e & 0xFF), EnvPeriodHigh = (byte)((e >> 8) & 0xFF),
            NoisePeriod = (byte)(n & 0x1F)
        };
    }

    /// <summary>
    ///     Copies the frame's register state into a destination buffer.
    /// </summary>
    public void CopyTo(Span<byte> destination, int count = 14)
    {
        destination[0] = PeriodLowA; destination[1] = PeriodHighA;
        destination[2] = PeriodLowB; destination[3] = PeriodHighB;
        destination[4] = PeriodLowC; destination[5] = PeriodHighC;
        destination[6] = NoisePeriod; destination[7] = Mixer;
        destination[8] = VolumeA; destination[9] = VolumeB; destination[10] = VolumeC;
        destination[11] = EnvPeriodLow; destination[12] = EnvPeriodHigh; destination[13] = EnvShape;
        if (count > 14) destination[14] = EffectType;
        if (count > 15) destination[15] = EffectData;
    }

    /// <summary>
    ///     Calculates a bitmask of registers that have changed since the last frame.
    /// </summary>
    public ushort GetDeltaMask(YmFrame last, bool isFirstFrame)
    {
        if (isFirstFrame) return 0x3FFF;
        ushort mask = 0;
        if (PeriodLowA != last.PeriodLowA) mask |= 1 << 0;
        if (PeriodHighA != last.PeriodHighA) mask |= 1 << 1;
        if (PeriodLowB != last.PeriodLowB) mask |= 1 << 2;
        if (PeriodHighB != last.PeriodHighB) mask |= 1 << 3;
        if (PeriodLowC != last.PeriodLowC) mask |= 1 << 4;
        if (PeriodHighC != last.PeriodHighC) mask |= 1 << 5;
        if (NoisePeriod != last.NoisePeriod) mask |= 1 << 6;
        if (Mixer != last.Mixer) mask |= 1 << 7;
        if (VolumeA != last.VolumeA) mask |= 1 << 8;
        if (VolumeB != last.VolumeB) mask |= 1 << 9;
        if (VolumeC != last.VolumeC) mask |= 1 << 10;
        if (EnvPeriodLow != last.EnvPeriodLow) mask |= 1 << 11;
        if (EnvPeriodHigh != last.EnvPeriodHigh) mask |= 1 << 12;
        if (EnvShape != last.EnvShape || ForceEnvReset) mask |= 1 << 13;
        return mask;
    }
}

/// <summary>
///     Represents the complete state and pattern-logic of a YM music asset.
/// </summary>
internal record YmMusic(List<YmFrame> Frames)
{
    /// <summary>
    ///     Finds the optimal pattern size for compression by trying multiple candidates.
    /// </summary>
    public byte[] Optimize(int manualPatSize, out int bestSize, out int u, out int s)
    {
        if (manualPatSize > 0)
        {
            bestSize = manualPatSize;
            return Compress(manualPatSize, out u, out s);
        }

        Console.WriteLine("Optimizing pattern size...");
        byte[]? bestData = null;
        bestSize = u = s = 0;
        int[] patternSizes = [16, 32, 48, 64, 80, 96, 128, 160, 192, 255];
        foreach (var size in patternSizes)
        {
            try
            {
                var trial = Compress(size, out var tu, out var ts);
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

    /// <summary>
    ///     Compresses the music into a pattern-based format with delta-masking.
    /// </summary>
    public byte[] Compress(int patSize, out int uniqueCount, out int seqLen)
    {
        var numBlocks = (int)Math.Ceiling((double)Frames.Count / patSize);
        if (numBlocks > 255) throw new InvalidOperationException("Song too long.");

        var uniquePatterns = new List<List<YmFrame>>();
        var sequence = BuildPatternSequence(patSize, numBlocks, uniquePatterns);

        uniqueCount = uniquePatterns.Count;
        seqLen = sequence.Count;

        var output = new List<byte> { (byte)patSize, (byte)uniqueCount, (byte)seqLen };
        output.AddRange(sequence.Select(id => (byte)id));

        WriteCompressedPatterns(output, uniquePatterns);
        return output.ToArray();
    }

    /// <summary>
    ///     Identifies unique patterns and builds the sequence table.
    /// </summary>
    private List<int> BuildPatternSequence(int patSize, int numBlocks, List<List<YmFrame>> uniquePatterns)
    {
        var sequence = new List<int>();
        var lookup = new Dictionary<List<YmFrame>, int>(new PatternComparer());

        for (var b = 0; b < numBlocks; b++)
        {
            var framesInBlock = Math.Min(patSize, Frames.Count - b * patSize);
            var block = Frames.GetRange(b * patSize, framesInBlock);
            if (!lookup.TryGetValue(block, out var id))
            {
                id = uniquePatterns.Count;
                uniquePatterns.Add(block);
                lookup[block] = id;
            }
            sequence.Add(id);
        }
        return sequence;
    }

    /// <summary>
    ///     Compresses unique patterns and writes them to the output buffer along with an offset table.
    /// </summary>
    private static void WriteCompressedPatterns(List<byte> output, List<List<YmFrame>> uniquePatterns)
    {
        var compressedPatterns = uniquePatterns.Select(p => CompressPattern(p)).ToList();
        var offset = 0;
        foreach (var cp in compressedPatterns)
        {
            output.Add((byte)(offset & 0xFF));
            output.Add((byte)((offset >> 8) & 0xFF));
            offset += cp.Length;
        }
        output.AddRange(compressedPatterns.SelectMany(x => x));
    }

    /// <summary>
    ///     Compresses a single pattern using delta-masking for each frame.
    /// </summary>
    private static byte[] CompressPattern(List<YmFrame> patternFrames)
    {
        var data = new List<byte>();
        var last = new YmFrame();
        var working = new byte[16];
        for (var f = 0; f < patternFrames.Count; f++)
        {
            var current = patternFrames[f];
            var mask = current.GetDeltaMask(last, f == 0);
            data.Add((byte)(mask & 0xFF));
            data.Add((byte)((mask >> 8) & 0xFF));
            current.CopyTo(working);
            for (var r = 0; r < 14; r++)
                if ((mask & (1 << r)) != 0) data.Add(working[r]);
            last = current;
        }
        return data.ToArray();
    }

    /// <summary>
    ///     Calculates the PHI2-based loop delay values (Y and X) for a target playback frequency.
    /// </summary>
    public static (int y, int x) CalculateDelay(int hz)
    {
        var remaining = Math.Max(0, 1789773.0 / hz - 1800.0);
        var y = Math.Max(1, (int)Math.Floor(remaining / 1285.0));
        var x = (int)Math.Min(255, Math.Round((remaining - y * 1285.0) / 5.0));
        return (y, x);
    }

    /// <summary>
    ///     Saves the compressed binary and the sidecar metadata file.
    /// </summary>
    public static void Save(string bin, string inc, string src, byte[] data, int frames, int y, int x, int u, int s, int p, int hz)
    {
        File.WriteAllBytes(bin, data);
        File.WriteAllText(inc, $"; Config for {src}\nMAX_FRAMES   = {frames}\nPLAYER_HZ    = {hz}\nYM_DELAY     = {y}\nYM_FINE      = {x}\nPATTERN_SIZE = {p}\nNUM_PATTERNS = {u}\nSEQ_LEN      = {s}\n");
        Console.WriteLine($"Wrote {data.Length} bytes. Size: {p}, Unique: {u}, Seq: {s}. Ratio: {data.Length / (double)(frames * 14) * 100:F1}%");
    }

    /// <summary>
    ///     Renders the music asset to a 16-bit PCM Mono WAV file.
    /// </summary>
    public void SaveWav(string filePath, int hz)
    {
        const int sampleRate = 44100;
        const double clock = 1792000.0;
        var emu = new AymEmulator(clock, sampleRate);
        var samplesPerFrame = sampleRate / hz;
        var totalSamples = (long)Frames.Count * samplesPerFrame;

        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        WriteRiffHeader(bw, totalSamples, sampleRate);
        RenderFramesToWav(bw, emu, samplesPerFrame);
    }

    /// <summary>
    ///     Writes the RIFF/WAVE header to the stream.
    /// </summary>
    private static void WriteRiffHeader(BinaryWriter bw, long totalSamples, int sampleRate)
    {
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write((uint)(36 + totalSamples * 2));
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write((uint)16); bw.Write((ushort)1); bw.Write((ushort)1);
        bw.Write((uint)sampleRate); bw.Write((uint)(sampleRate * 2));
        bw.Write((ushort)2); bw.Write((ushort)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write((uint)(totalSamples * 2));
    }

    /// <summary>
    ///     Drives the PSG emulator to render each music frame into raw PCM audio samples.
    /// </summary>
    private void RenderFramesToWav(BinaryWriter bw, AymEmulator emu, int samplesPerFrame)
    {
        var working = new byte[16];
        foreach (var frame in Frames)
        {
            frame.CopyTo(working, 16);
            emu.UpdateRegisters(working);
            for (var s = 0; s < samplesPerFrame; s++)
                bw.Write(emu.RenderSample());
        }
    }
}

internal sealed class PatternComparer : IEqualityComparer<List<YmFrame>>
{
    /// <summary>
    ///     Determines if two frame patterns are identical by checking every frame register state.
    /// </summary>
    public bool Equals(List<YmFrame>? x, List<YmFrame>? y) => x != null && y != null && x.SequenceEqual(y);

    /// <summary>
    ///     Calculates a combined hash code for a frame pattern.
    /// </summary>
    public int GetHashCode(List<YmFrame> obj)
    {
        var h = new HashCode();
        foreach (var f in obj) h.Add(f);
        return h.ToHashCode();
    }
}

internal static class CommandLineUtils
{
    /// <summary>
    ///     Checks if a specific tool (like 7z) is available in the system's PATH.
    /// </summary>
    public static bool IsToolInstalled(string toolName)
    {
        try
        {
            var searchCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            using var p = Process.Start(new ProcessStartInfo(searchCmd, toolName) { RedirectStandardOutput = true, UseShellExecute = false });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }
}
