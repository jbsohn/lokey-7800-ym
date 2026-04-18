namespace Core;

/// <summary>
///     Represents a single audio frame of YM2149 / YM6 register data.
/// </summary>
public readonly record struct YmFrame(
    byte PeriodLowA,
    byte PeriodHighA,
    byte PeriodLowB,
    byte PeriodHighB,
    byte PeriodLowC,
    byte PeriodHighC,
    byte NoisePeriod,
    byte Mixer,
    byte VolumeA,
    byte VolumeB,
    byte VolumeC,
    byte EnvPeriodLow,
    byte EnvPeriodHigh,
    byte EnvShape,
    byte EffectType = 0,
    byte EffectData = 0,
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
    )
    {
    }

    // Hardware Property Mappings
    private ushort ToneA => (ushort)(((PeriodHighA & 0x0F) << 8) | PeriodLowA);
    private ushort ToneB => (ushort)(((PeriodHighB & 0x0F) << 8) | PeriodLowB);
    private ushort ToneC => (ushort)(((PeriodHighC & 0x0F) << 8) | PeriodLowC);
    private ushort EnvPeriod => (ushort)((EnvPeriodHigh << 8) | EnvPeriodLow);

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

        return this with
        {
            PeriodLowA = (byte)(tA & 0xFF),
            PeriodHighA = (byte)((tA >> 8) & 0x0F),
            PeriodLowB = (byte)(tB & 0xFF),
            PeriodHighB = (byte)((tB >> 8) & 0x0F),
            PeriodLowC = (byte)(tC & 0xFF),
            PeriodHighC = (byte)((tC >> 8) & 0x0F),
            EnvPeriodLow = (byte)(e & 0xFF),
            EnvPeriodHigh = (byte)((e >> 8) & 0xFF),
            NoisePeriod = (byte)(n & 0x1F)
        };
    }

    /// <summary>
    ///     Copies the frame's register state into a destination buffer.
    /// </summary>
    public void CopyTo(Span<byte> destination, int count = 14)
    {
        destination[0] = PeriodLowA;
        destination[1] = PeriodHighA;
        destination[2] = PeriodLowB;
        destination[3] = PeriodHighB;
        destination[4] = PeriodLowC;
        destination[5] = PeriodHighC;
        destination[6] = NoisePeriod;
        destination[7] = Mixer;
        destination[8] = VolumeA;
        destination[9] = VolumeB;
        destination[10] = VolumeC;
        destination[11] = EnvPeriodLow;
        destination[12] = EnvPeriodHigh;
        destination[13] = EnvShape;
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
