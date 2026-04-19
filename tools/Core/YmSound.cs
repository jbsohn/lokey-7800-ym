namespace Core;

/// <summary>
///     Defines the different types of audio tracks available for the YM2149.
/// </summary>
public enum TrackType
{
    /// <summary> Channel A: Registers 0, 1, 8 </summary>
    ChannelA,
    /// <summary> Channel B: Registers 2, 3, 9 </summary>
    ChannelB,
    /// <summary> Channel C: Registers 4, 5, 10 </summary>
    ChannelC,
    /// <summary> Global: Registers 6, 7, 11, 12, 13 </summary>
    Global,
    /// <summary> Combined Channel C and Global: Registers 4, 5, 6, 7, 10, 11, 12, 13 </summary>
    ChannelCPlusGlobal
}

/// <summary>
///     Provides mappings between track types and their corresponding hardware registers.
/// </summary>
public static class TrackMappings
{
    public static readonly int[] ChannelA = { 0, 1, 8 };
    public static readonly int[] ChannelB = { 2, 3, 9 };
    public static readonly int[] ChannelC = { 4, 5, 10 };
    public static readonly int[] Global = { 6, 7, 11, 12, 13 };
    public static readonly int[] ChannelCPlusGlobal = { 4, 5, 6, 7, 10, 11, 12, 13 };

    /// <summary>
    ///     Returns the array of register indices for a given track type.
    /// </summary>
    public static int[] GetRegisters(TrackType type) => type switch
    {
        TrackType.ChannelA => ChannelA,
        TrackType.ChannelB => ChannelB,
        TrackType.ChannelC => ChannelC,
        TrackType.Global => Global,
        TrackType.ChannelCPlusGlobal => ChannelCPlusGlobal,
        _ => Array.Empty<int>()
    };
}

/// <summary>
///     Represents a surgical, per-track sequence of YM2149 register updates.
/// </summary>
public record YmSound(string Name, TrackType Track, List<YmFrame> Frames)
{
    /// <summary>
    ///     Serializes the sound into a delta-masked bitstream suitable for the 6502 replayer.
    /// </summary>
    public byte[] Serialize()
    {
        var registers = TrackMappings.GetRegisters(Track);
        var data = new List<byte>();
        var last = new YmFrame();

        for (var f = 0; f < Frames.Count; f++)
        {
            var current = Frames[f];
            byte mask = 0;
            var frameData = new List<byte>();

            for (var i = 0; i < registers.Length; i++)
            {
                var r = registers[i];
                var val = current.GetRegister(r);
                
                // Change detection: first frame always writes, subsequent frames only on change.
                // R13 (Envelope Shape) is special and often needs forcing.
                var changed = f == 0 || val != last.GetRegister(r);
                if (r == 13 && current.ForceEnvReset) changed = true;

                if (changed)
                {
                    mask |= (byte)(1 << i);
                    frameData.Add(val);
                }
            }

            data.Add(mask);
            data.AddRange(frameData);
            last = current;
        }

        return data.ToArray();
    }

    /// <summary>
    ///     Serializes the sound into a 14-register interleaved stream (Legacy Format).
    ///     Ensures only registers assigned to the sound's track are included in the mask.
    /// </summary>
    public byte[] SerializeSimple()
    {
        var data = new List<byte>();
        var last = new YmFrame();
        var allowedRegisters = TrackMappings.GetRegisters(Track);

        for (var f = 0; f < Frames.Count; f++)
        {
            var current = Frames[f];
            var fullMask = current.GetDeltaMask(last, f == 0);
            
            // Surgical Masking: Only keep bits for registers allowed on this track
            ushort surgicalMask = 0;
            for (int r = 0; r < 14; r++)
            {
                if (allowedRegisters.Contains(r) && (fullMask & (1 << r)) != 0)
                {
                    surgicalMask |= (ushort)(1 << r);
                }
            }
            
            data.Add((byte)(surgicalMask & 0xFF));
            data.Add((byte)((surgicalMask >> 8) & 0xFF));
            
            for (var r = 0; r < 14; r++)
            {
                if ((surgicalMask & (1 << r)) != 0)
                {
                    data.Add(current.GetRegister(r));
                }
            }
            last = current;
        }

        return data.ToArray();
    }

    /// <summary>
    ///     Returns the sound as a series of DASM-compatible dc.b statements.
    /// </summary>
    public string ToAssembly(bool simple = true, int bytesPerLine = 16)
    {
        var bytes = simple ? SerializeSimple() : Serialize();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"; YmSound: {Name} ({Track})");
        sb.AppendLine($"{Name}_Start:");
        
        for (int i = 0; i < bytes.Length; i += bytesPerLine)
        {
            var chunk = bytes.Skip(i).Take(bytesPerLine).Select(b => $"${b:X2}");
            sb.AppendLine($"    dc.b {string.Join(",", chunk)}");
        }
        
        sb.AppendLine($"{Name}_End:");
        sb.AppendLine($"{Name}_Length = {Name}_End - {Name}_Start");
        return sb.ToString();
    }
}
