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
);
