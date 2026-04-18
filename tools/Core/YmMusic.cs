using System.Text;

namespace Core;

/// <summary>
///     Represents the complete state and pattern-logic of a YM music asset.
/// </summary>
public record YmMusic(List<YmFrame> Frames)
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
            try
            {
                var trial = Compress(size, out var tu, out var ts);
                Console.WriteLine($"  Size {size,3}: {trial.Length,6} bytes ({tu,3} unique)");
                if (bestData == null || trial.Length < bestData.Length)
                    (bestData, bestSize, u, s) = (trial, size, tu, ts);
            }
            catch (InvalidOperationException)
            {
            }

        return bestData ?? throw new InvalidOperationException("Optimization failed.");
    }

    /// <summary>
    ///     Compresses the music into a pattern-based format with delta-masking.
    /// </summary>
    private byte[] Compress(int patSize, out int uniqueCount, out int seqLen)
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
        var compressedPatterns = uniquePatterns.Select(CompressPattern).ToList();
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
                if ((mask & (1 << r)) != 0)
                    data.Add(working[r]);
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
        var yRaw = (int)Math.Floor(remaining / 1285.0);
        var x = (int)Math.Clamp(Math.Round((remaining - yRaw * 1285.0) / 5.0), 0, 255);
        return (Math.Max(1, yRaw), x);
    }

    /// <summary>
    ///     Saves the compressed binary and the sidecar metadata file.
    /// </summary>
    public static void Save(string bin, string inc, string src, byte[] data, int frames, int y, int x, int u, int s,
        int p, int hz)
    {
        File.WriteAllBytes(bin, data);
        File.WriteAllText(inc,
            $"; Config for {src}\nMAX_FRAMES   = {frames}\nPLAYER_HZ    = {hz}\nYM_DELAY     = {y}\nYM_FINE      = {x}\nPATTERN_SIZE = {p}\nNUM_PATTERNS = {u}\nSEQ_LEN      = {s}\n");
        Console.WriteLine(
            $"Wrote {data.Length} bytes. Size: {p}, Unique: {u}, Seq: {s}. Ratio: {data.Length / (double)(frames * 14) * 100:F1}%");
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
        bw.Write((uint)16);
        bw.Write((ushort)1);
        bw.Write((ushort)1);
        bw.Write((uint)sampleRate);
        bw.Write((uint)(sampleRate * 2));
        bw.Write((ushort)2);
        bw.Write((ushort)16);
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
    public bool Equals(List<YmFrame>? x, List<YmFrame>? y)
    {
        return x != null && y != null && x.SequenceEqual(y);
    }

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
