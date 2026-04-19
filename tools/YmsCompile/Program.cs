using System.Text.Json;
using Core;

namespace YmsCompile;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 1 || args.Any(a => a is "-h" or "--help"))
        {
            Console.WriteLine("Usage: YmsCompile <recipe.json> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -o <file>   Output file (.yms or .asm)");
            Console.WriteLine("  -asm        Output as DASM assembly");
            return 1;
        }

        var input = args[0];
        var asm = args.Contains("-asm");
        string? output = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length) output = args[i + 1];
        }

        if (output == null)
        {
            output = Path.ChangeExtension(input, asm ? ".asm" : ".yms");
        }

        try
        {
            var json = File.ReadAllText(input);
            var recipe = JsonSerializer.Deserialize<YmSoundRecipe>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            if (recipe == null) throw new Exception("Failed to deserialize recipe.");

            var sound = recipe.Bake();

            if (asm)
            {
                File.WriteAllText(output, sound.ToAssembly(simple: true));
                Console.WriteLine($"Compiled assembly to {output}");
            }
            else
            {
                var serialized = sound.SerializeSimple();
                // Add two null bytes as an end sentinel for the 16-bit mask player
                var final = new byte[serialized.Length + 2];
                Array.Copy(serialized, 0, final, 0, serialized.Length);
                File.WriteAllBytes(output, final);
                Console.WriteLine($"Compiled {final.Length} bytes to {output} (Legacy Format)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
