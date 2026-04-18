# nullable enable
using System;

/// <summary>
///     Literal C# port of aym-js by Olivier PONCET.
///     Synchronized with the js7800 emulator fork.
/// </summary>
internal class AymEmulator
{
    private static readonly float[] YmDac = {
        0.0000000f, 0.0000000f, 0.0046540f, 0.0077211f, 0.0109560f, 0.0139620f, 0.0169986f, 0.0200198f,
        0.0243687f, 0.0296941f, 0.0350652f, 0.0403906f, 0.0485389f, 0.0583352f, 0.0680552f, 0.0777752f,
        0.0925154f, 0.1110857f, 0.1297475f, 0.1484855f, 0.1766690f, 0.2115511f, 0.2463874f, 0.2811017f,
        0.3337301f, 0.4004273f, 0.4673838f, 0.5344320f, 0.6351720f, 0.7580072f, 0.8799268f, 1.0000000f
    };

    private readonly EnvelopeGenerator _env = new();
    private readonly NoiseGenerator _noise = new();
    private readonly double _psgClock, _sampleRate;
    private readonly byte[] _regs = new byte[16];
    private readonly ToneGenerator[] _tones = { new(), new(), new() };
    private double _ticksAccumulator;

    public AymEmulator(double clk, double rate)
    {
        _sampleRate = rate;
        _psgClock = clk / 8.0;
    }

    public void UpdateRegisters(ReadOnlySpan<byte> r)
    {
        for (var i = 0; i < 14; i++)
        {
            if (_regs[i] == r[i] && i != 13) continue;
            _regs[i] = r[i];
            if (i < 6) _tones[i / 2].Period = ((_regs[i / 2 * 2 + 1] & 0x0F) << 8) | _regs[i / 2 * 2];
            else if (i == 6) _noise.Period = _regs[6] & 0x1F;
            else if (i == 11 || i == 12) _env.Period = (_regs[12] << 8) | _regs[11];
            else if (i == 13) _env.Reset(_regs[13] & 0x0F);
        }
    }

    public short RenderSample()
    {
        var step = _psgClock / _sampleRate;
        _ticksAccumulator += step;
        while (_ticksAccumulator >= 1.0)
        {
            foreach (var t in _tones) t.Clock();
            _noise.Clock();
            _env.Clock();
            _ticksAccumulator -= 1.0;
        }

        double mixed = 0;
        for (var i = 0; i < 3; i++)
        {
            var toneEn = (_regs[7] & (1 << i)) == 0;
            var noiseEn = (_regs[7] & (1 << (i + 3))) == 0;
            var chanOut = (toneEn || _tones[i].Phase != 0) && (noiseEn || _noise.Phase != 0);
            if (chanOut)
            {
                var vol = _regs[8 + i] & 0x0F;
                var envEn = (_regs[8 + i] & 0x10) != 0;
                mixed += YmDac[envEn ? _env.Level : vol * 2 + 1];
            }
        }
        return (short)(Math.Clamp(mixed / 3.0, -1.0, 1.0) * 16384);
    }

    private class ToneGenerator
    {
        public int Period, Counter, Phase = 1;
        public void Clock()
        {
            if (++Counter >= (Period == 0 ? 1 : Period)) { Counter = 0; Phase ^= 1; }
        }
    }

    private class NoiseGenerator
    {
        public int Period, Counter, Phase = 1, Lfsr = 1;
        public void Clock()
        {
            if (++Counter >= (Period == 0 ? 1 : Period))
            {
                Counter = 0;
                var b0 = Lfsr & 1;
                var b3 = (Lfsr >> 3) & 1;
                Lfsr = (Lfsr >> 1) | ((b0 ^ b3) << 16);
                Phase = Lfsr & 1;
            }
        }
    }

    private class EnvelopeGenerator
    {
        public int Period, Counter, Shape, Level, Phase;
        private bool _hold;
        public void Reset(int shape) { Shape = shape; Counter = 0; Level = (shape & 4) != 0 ? 0 : 31; Phase = 0; _hold = false; }
        public void Clock()
        {
            if (_hold) return;
            if (++Counter >= (Period == 0 ? 1 : Period))
            {
                Counter = 0;
                var attack = (Shape & 4) != 0;
                var alt = (Shape & 2) != 0;
                var hold = (Shape & 1) != 0;
                var cont = (Shape & 8) != 0;
                if (Phase == 0)
                {
                    Level = attack ? Level + 1 : Level - 1;
                    if (Level < 0 || Level > 31)
                    {
                        if (!cont) { Level = 0; _hold = true; }
                        else if (hold) { Level = alt ^ attack ? 0 : 31; _hold = true; }
                        else { if (alt) Shape ^= 4; Phase = 0; Level = (Shape & 4) != 0 ? 0 : 31; }
                    }
                }
            }
        }
    }
}
