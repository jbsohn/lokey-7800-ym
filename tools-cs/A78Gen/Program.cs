using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A78Gen;

/// Header offset-64 "Mapper" values. See docs/Hardware-32pin.md and
/// docs/Emulation.md for the corresponding emulator-side detection.
public static class Mapper
{
    public const byte Linear = 0;
    /// 32-pin board: fixed 32K @ $8000-$FFFF + 16K @ $4000-$7FFF banked
    /// via the YM2149 IOA port. Input binary must be the full 128K or 256K
    /// ROM image (not just the fixed bank).
    public const byte YmBanked = 1;
}

public class A78Config
{
    [JsonPropertyName("version")] public byte Version { get; set; } = 4;
    [JsonPropertyName("title")] public string Title { get; set; } = "YM2149 GAME";
    [JsonPropertyName("cart_type")] public ushort CartType { get; set; } = 0;
    [JsonPropertyName("controller_1")] public byte Controller1 { get; set; } = 1;
    [JsonPropertyName("controller_2")] public byte Controller2 { get; set; } = 1;
    [JsonPropertyName("tv_type")] public byte TvType { get; set; } = 0;
    [JsonPropertyName("save_device")] public byte SaveDevice { get; set; } = 0;
    [JsonPropertyName("slot_passthrough")] public byte SlotPassthrough { get; set; } = 0;
    [JsonPropertyName("mapper")] public byte Mapper { get; set; } = 0;
    [JsonPropertyName("mapper_opts")] public byte MapperOpts { get; set; } = 0;
    [JsonPropertyName("audio")] public ushort Audio { get; set; } = 0x0800;
    [JsonPropertyName("interrupt")] public ushort Interrupt { get; set; } = 0;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(A78Config))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: a78gen <input.bin> <config.json> -o <output.a78>");
            Environment.Exit(1);
        }

        string inputPath = args[0];
        string configPath = args[1];
        string outputPath = "output.a78";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length) outputPath = args[++i];
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input binary '{inputPath}' not found.");
            Environment.Exit(1);
        }

        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"Error: Configuration file '{configPath}' not found.");
            Environment.Exit(1);
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.A78Config) ?? new A78Config();

        byte[] rawData = File.ReadAllBytes(inputPath);
        byte[] romData;

        if (config.Mapper == Mapper.YmBanked)
        {
            // Fixed 32K + YM-IOA-banked 16K: ship the whole image untouched.
            // The board only wires up a 128K (AT27C010) or 256K
            // (AT27C020, or AT27C040 used as top-half-only) chip.
            if (rawData.Length != 128 * 1024 && rawData.Length != 256 * 1024)
            {
                Console.Error.WriteLine(
                    $"Error: mapper 1 (YM-IOA banked) requires a 128KB or 256KB input binary, got {rawData.Length} bytes.");
                Environment.Exit(1);
            }
            romData = rawData;
        }
        else
        {
            if (rawData.Length > 32768)
            {
                Console.WriteLine(
                    $"Warning: input is {rawData.Length} bytes but mapper is 0 (linear/fixed 32K) -- only the top 32KB will be kept. Set \"mapper\": 1 in the config to build a banked image instead.");
            }
            romData = new byte[32768];
            Array.Fill(romData, (byte)0xFF);
            int copyLength = Math.Min(32768, rawData.Length);
            Array.Copy(rawData, rawData.Length - copyLength, romData, 32768 - copyLength, copyLength);
        }

        string romPath = Path.ChangeExtension(outputPath, ".rom");
        File.WriteAllBytes(romPath, romData);

        byte[] header = new byte[128];
        header[0] = config.Version;
        Encoding.ASCII.GetBytes("ATARI7800").CopyTo(header, 1);

        byte[] titleBytes = Encoding.ASCII.GetBytes(config.Title.PadRight(32));
        Array.Copy(titleBytes, 0, header, 17, 32);

        header[49] = (byte)(romData.Length >> 24);
        header[50] = (byte)(romData.Length >> 16);
        header[51] = (byte)(romData.Length >> 8);
        header[52] = (byte)(romData.Length & 0xFF);

        header[53] = (byte)(config.CartType >> 8);
        header[54] = (byte)(config.CartType & 0xFF);

        header[55] = config.Controller1;
        header[56] = config.Controller2;
        header[57] = config.TvType;
        header[58] = config.SaveDevice;
        header[63] = config.SlotPassthrough;
        header[64] = config.Mapper;
        header[65] = config.MapperOpts;
        header[66] = (byte)(config.Audio >> 8);
        header[67] = (byte)(config.Audio & 0xFF);
        header[68] = (byte)(config.Interrupt >> 8);
        header[69] = (byte)(config.Interrupt & 0xFF);

        Encoding.ASCII.GetBytes("ACTUAL CART DATA STARTS HERE").CopyTo(header, 100);

        using var fs = new FileStream(outputPath, FileMode.Create);
        fs.Write(header, 0, 128);
        fs.Write(romData, 0, romData.Length);

        Console.WriteLine($"Generated {outputPath} and {romPath}");
    }
}
