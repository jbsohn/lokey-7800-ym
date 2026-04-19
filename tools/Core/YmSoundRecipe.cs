using System.Text.Json.Serialization;

namespace Core;

/// <summary>
///     Represents a high-level command in a sound recipe.
/// </summary>
public class SoundCommand
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "wait";
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 1;
    
    [JsonPropertyName("pitch")]
    public int? Pitch { get; set; }
    
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }
    
    [JsonPropertyName("target_pitch")]
    public int? TargetPitch { get; set; }
    
    [JsonPropertyName("target_volume")]
    public int? TargetVolume { get; set; }

    [JsonPropertyName("noise")]
    public int? Noise { get; set; }

    [JsonPropertyName("mixer")]
    public int? Mixer { get; set; }
}

/// <summary>
///     A human-readable recipe for a YM sound effect.
/// </summary>
public class YmSoundRecipe
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "NewSound";
    
    [JsonPropertyName("track")]
    public TrackType Track { get; set; } = TrackType.ChannelC;
    
    [JsonPropertyName("commands")]
    public List<SoundCommand> Commands { get; set; } = new();

    /// <summary>
    ///     "Bakes" the procedural recipe into a sequence of raw YM frames.
    /// </summary>
    public YmSound Bake()
    {
        var frames = new List<YmFrame>();
        var currentRegs = new byte[16];
        bool r13Trigger = false;

        // Initial state: Silence and middle pitch
        currentRegs[7] = 0x38; // Default mixer (all off)
        
        foreach (var cmd in Commands)
        {
            switch (cmd.Type.ToLower())
            {
                case "set":
                    ApplySet(cmd, currentRegs, ref r13Trigger);
                    frames.Add(new YmFrame(currentRegs, r13Trigger));
                    r13Trigger = false;
                    break;

                case "wait":
                    for (int i = 0; i < cmd.Duration; i++)
                    {
                        frames.Add(new YmFrame(currentRegs, false));
                    }
                    break;

                case "slide":
                    BakeSlide(cmd, frames, currentRegs);
                    break;

                case "fade":
                    BakeFade(cmd, frames, currentRegs);
                    break;
            }
        }

        return new YmSound(Name, Track, frames);
    }

    private void ApplySet(SoundCommand cmd, byte[] regs, ref bool r13)
    {
        if (cmd.Pitch.HasValue)
        {
            var p = cmd.Pitch.Value;
            regs[0] = regs[2] = regs[4] = (byte)(p & 0xFF);
            regs[1] = regs[3] = regs[5] = (byte)((p >> 8) & 0x0F);
        }
        if (cmd.Volume.HasValue)
        {
            regs[8] = regs[9] = regs[10] = (byte)(cmd.Volume.Value & 0x0F);
        }
        if (cmd.Noise.HasValue)
        {
            regs[6] = (byte)(cmd.Noise.Value & 0x1F);
        }
        if (cmd.Mixer.HasValue)
        {
            regs[7] = (byte)cmd.Mixer.Value;
        }
    }

    private void BakeSlide(SoundCommand cmd, List<YmFrame> frames, byte[] regs)
    {
        if (!cmd.TargetPitch.HasValue) return;
        
        int startPitch = ((regs[1] & 0x0F) << 8) | regs[0]; // Simplified: assume all tracks same for recipe
        int endPitch = cmd.TargetPitch.Value;
        int duration = Math.Max(1, cmd.Duration);

        for (int i = 1; i <= duration; i++)
        {
            double t = (double)i / duration;
            int p = (int)(startPitch + (endPitch - startPitch) * t);
            regs[0] = regs[2] = regs[4] = (byte)(p & 0xFF);
            regs[1] = regs[3] = regs[5] = (byte)((p >> 8) & 0x0F);
            frames.Add(new YmFrame(regs, false));
        }
    }

    private void BakeFade(SoundCommand cmd, List<YmFrame> frames, byte[] regs)
    {
        if (!cmd.TargetVolume.HasValue) return;

        int startVol = regs[8] & 0x0F;
        int endVol = cmd.TargetVolume.Value;
        int duration = Math.Max(1, cmd.Duration);

        for (int i = 1; i <= duration; i++)
        {
            double t = (double)i / duration;
            int v = (int)(startVol + (endVol - startVol) * t);
            regs[8] = regs[9] = regs[10] = (byte)(v & 0x0F);
            frames.Add(new YmFrame(regs, false));
        }
    }
}
