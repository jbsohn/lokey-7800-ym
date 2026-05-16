using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A78Gen;

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
    [JsonPropertyName("audio")] public ushort Audio { get; set; } = 0x4000;
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
        byte[] romData = new byte[32768];
        Array.Fill(romData, (byte)0xFF);
        int copyLength = Math.Min(32768, rawData.Length);
        Array.Copy(rawData, rawData.Length - copyLength, romData, 32768 - copyLength, copyLength);

        string romPath = Path.ChangeExtension(outputPath, ".rom");
        File.WriteAllBytes(romPath, romData);

        byte[] header = new byte[128];
        header[0] = config.Version;
        Encoding.ASCII.GetBytes("ATARI7800").CopyTo(header, 1);
        
        byte[] titleBytes = Encoding.ASCII.GetBytes(config.Title.PadRight(32));
        Array.Copy(titleBytes, 0, header, 17, 32);
        
        header[49] = 0; header[50] = 0; header[51] = 0x80; header[52] = 0x00;
        
        header[53] = (byte)(config.CartType >> 8);
        // Force YM bit (bit 2) in low byte of Cart Type for older emulator support
        header[54] = (byte)((config.CartType & 0xFF) | 0x04); 
        
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

        // --- CHECKSUM CALCULATION ---
        // Some emulators require the 128-byte header to be checksummed
        long sum = 0;
        for (int i = 0; i < 127; i++) sum += header[i];
        header[127] = (byte)(sum & 0xFF);

        using var fs = new FileStream(outputPath, FileMode.Create);
        fs.Write(header, 0, 128);
        fs.Write(romData, 0, 32768);

        Console.WriteLine($"Generated {outputPath} and {romPath}");
    }
}
