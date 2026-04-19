using System.Buffers.Binary;
using System.Text;
using Core;

namespace YmsToWav;

internal static class Program
{
    private const int SampleRate = 44100;
    private const double ClockSpeed = 1792000.0;

    public static int Main(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine("Usage: YmsToWav <input.yms> [output.wav] [hz]");
            return 1;
        }

        var input = args[0];
        var output = args.Length > 1 ? args[1] : Path.ChangeExtension(input, ".wav");
        var hz = args.Length > 2 ? int.Parse(args[2]) : 50;

        try
        {
            var data = File.ReadAllBytes(input);
            var emu = new AymEmulator(ClockSpeed, SampleRate);
            var samplesPerFrame = SampleRate / hz;
            
            var regs = new byte[16];
            var musicPtr = 0;
            var frames = 0;
            
            // First pass to count frames
            while (musicPtr + 2 <= data.Length)
            {
                var mask = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(musicPtr));
                if (mask == 0) break;
                musicPtr += 2;
                for (int r = 0; r < 14; r++)
                    if ((mask & (1 << r)) != 0) musicPtr++;
                frames++;
            }

            var totalSamples = (long)frames * samplesPerFrame;

            using var fs = File.Create(output);
            using var bw = new BinaryWriter(fs);

            WriteWavHeader(bw, totalSamples);

            musicPtr = 0;
            for (int f = 0; f < frames; f++)
            {
                var mask = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(musicPtr));
                musicPtr += 2;
                for (int r = 0; r < 14; r++)
                    if ((mask & (1 << r)) != 0)
                        regs[r] = data[musicPtr++];

                emu.UpdateRegisters(regs);
                for (int s = 0; s < samplesPerFrame; s++)
                    bw.Write(emu.RenderSample());
            }

            Console.WriteLine($"Wrote {frames} frames to {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void WriteWavHeader(BinaryWriter bw, long totalSamples)
    {
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write((uint)(36 + totalSamples * 2));
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write((uint)16);
        bw.Write((ushort)1);
        bw.Write((ushort)1);
        bw.Write((uint)SampleRate);
        bw.Write((uint)(SampleRate * 2));
        bw.Write((ushort)2);
        bw.Write((ushort)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write((uint)(totalSamples * 2));
    }
}
