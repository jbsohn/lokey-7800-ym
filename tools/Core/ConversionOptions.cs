using System.Globalization;

namespace Core;

/// <summary>
///     Arguments passed to the conversion orchestrators.
/// </summary>
public record struct ConversionOptions(
    string InputFile,
    string? OutputFile,
    int MaxFrames,
    int PatternSize,
    int Step,
    int? OverrideHz
)
{
    public static ConversionOptions ParseArgs(string[] args)
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
                case "-o": i++; output = args[i]; break;
                case "-f": i++; max = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-p": i++; pat = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-s": i++; step = int.Parse(args[i], CultureInfo.InvariantCulture); break;
                case "-hz": i++; hz = int.Parse(args[i], CultureInfo.InvariantCulture); break;
            }
        }

        return new ConversionOptions(input, output, max, pat, step, hz);
    }

    public static void PrintUsage(string name, string inputSpec)
    {
        Console.WriteLine($"Usage: {name} <{inputSpec}> [options]");
        Console.WriteLine(
            "Options:\n  -o <file>   Output binary\n  -f <frames> Max frames\n  -p <size>   Pattern size (0=auto)\n  -s <step>   Frame step\n  -hz <val>   Override Hz");
    }
}
