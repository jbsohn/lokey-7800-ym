using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Core;

namespace YmTestDrive;

internal static class Program
{
    private const int SampleRate = 44100;
    private const double ClockSpeed = 1792000.0;

    public static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: YmTestDrive <music.bin> <sfx.yms> <trigger_frame> [output.wav]");
            return 1;
        }

        var musicFile = args[0];
        var sfxFile = args[1];
        var triggerFrame = int.Parse(args[2]);
        var outputFile = args.Length > 3 ? args[3] : "test_drive.wav";

        try
        {
            var musicData = File.ReadAllBytes(musicFile);
            var sfxData = File.ReadAllBytes(sfxFile);
            var playerHz = DetectPlayerHz(Path.ChangeExtension(musicFile, ".ymi"));

            var emu = new AymEmulator(ClockSpeed, SampleRate);
            var samplesPerFrame = SampleRate / playerHz;

            // Decode Music Metadata
            int patSize = musicData[0] == 0 ? 256 : musicData[0];
            int numPatterns = musicData[1], seqLen = musicData[2];
            int totalFrames = seqLen * patSize;

            var sequence = musicData.AsSpan(3, seqLen);
            var offsetTableStart = 3 + seqLen;
            var offsets = new ushort[numPatterns];
            for (var i = 0; i < numPatterns; i++)
                offsets[i] = BinaryPrimitives.ReadUInt16LittleEndian(musicData.AsSpan(offsetTableStart + i * 2));
            var patDataStart = offsetTableStart + numPatterns * 2;

            using var fs = File.Create(outputFile);
            using var bw = new BinaryWriter(fs);
            WriteWavHeader(bw, (long)totalFrames * samplesPerFrame);

            var regs = new byte[16];
            int sfxPtr = 0;
            bool sfxActive = false;

            Console.WriteLine($"Driving: {musicFile} with {sfxFile} at frame {triggerFrame}...");
            Console.WriteLine($"Music length: {musicData.Length}, SFX length: {sfxData.Length}");

            int globalFrame = 0;
            foreach (var patId in sequence)
            {
                if (patId >= numPatterns) { Console.WriteLine($"Invalid patId {patId}"); break; }
                var musicPtr = patDataStart + offsets[patId];
                for (int f = 0; f < patSize; f++)
                {
                    // 1. Music Step
                    if (musicPtr + 2 <= musicData.Length)
                    {
                        var mask = BinaryPrimitives.ReadUInt16LittleEndian(musicData.AsSpan(musicPtr));
                        musicPtr += 2;
                        for (int r = 0; r < 14; r++)
                        {
                            if ((mask & (1 << r)) != 0)
                            {
                                if (musicPtr < musicData.Length)
                                    regs[r] = musicData[musicPtr++];
                            }
                        }
                    }

                    // 2. SFX Trigger check
                    if (globalFrame == triggerFrame) sfxActive = true;

                    // 3. SFX Step (Sequential Override)
                    if (sfxActive)
                    {
                        if (sfxPtr + 2 > sfxData.Length)
                        {
                            sfxActive = false;
                        }
                        else
                        {
                            var sMask = BinaryPrimitives.ReadUInt16LittleEndian(sfxData.AsSpan(sfxPtr));
                            if (sMask == 0)
                            {
                                sfxActive = false;
                            }
                            else
                            {
                                sfxPtr += 2;
                                for (int r = 0; r < 14; r++)
                                {
                                    if ((sMask & (1 << r)) != 0)
                                    {
                                        if (sfxPtr < sfxData.Length)
                                            regs[r] = sfxData[sfxPtr++];
                                        else
                                            sfxActive = false;
                                    }
                                }
                            }
                        }
                    }

                    // 4. Render
                    emu.UpdateRegisters(regs);
                    for (int s = 0; s < samplesPerFrame; s++) bw.Write(emu.RenderSample());

                    globalFrame++;
                }
            }

            Console.WriteLine($"Success! Saved to {outputFile}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int DetectPlayerHz(string incFile)
    {
        if (!File.Exists(incFile)) return 60;
        var hzMatch = Regex.Match(File.ReadAllText(incFile), @"PLAYER_HZ\s*=\s*(\d+)");
        return hzMatch.Success ? int.Parse(hzMatch.Groups[1].Value) : 60;
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
