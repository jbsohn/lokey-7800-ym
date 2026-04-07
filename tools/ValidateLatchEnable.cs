#!/usr/bin/env dotnet-script
using System;
using System.Collections.Generic;
using System.Diagnostics;

// YM2149 Latch Timing Diagnostic Script (V1.0)
// Focuses on timing between PHI2, YM_LE, and BDIR/BC1
//
// Mapped to Logic Analyzer:
// D0 = PHI2 (Clock)
// D1 = YM_LE (Latch Enable - Pin 11 of 74HCT373)
// D2 = BDIR (YM Pin 27)
// D3 = BC1  (YM Pin 29)

Console.WriteLine("Starting Sigrok Latch Timing Capture...");
Console.WriteLine("Mapping: D0=PHI2, D1=YM_LE, D2=BDIR, D3=BC1");
Console.WriteLine("-------------------------------------------------");

var psi = new ProcessStartInfo
{
    FileName = "sigrok-cli",
    Arguments = "--driver fx2lafw --config samplerate=2M --samples 5000000 --channels D0,D1,D2,D3 -O csv",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

int countPHI2 = 0, countLE = 0, countBDIR = 0, countBC1 = 0;
int lastLE = -1, lastBDIR = -1, lastBC1 = -1, lastPHI2 = -1;
int samplesProcessed = 0;

try 
{
    using var process = new Process { StartInfo = psi };
    process.OutputDataReceived += (s, e) => {
        if (string.IsNullOrWhiteSpace(e.Data)) return;
        var line = e.Data.Trim();
        if (line.StartsWith("D") || line.StartsWith("l")) return; 

        var parts = line.Split(',');
        if (parts.Length >= 4 && 
            int.TryParse(parts[0], out int d0) &&  // PHI2
            int.TryParse(parts[1], out int d1) &&  // YM_LE
            int.TryParse(parts[2], out int d2) &&  // BDIR
            int.TryParse(parts[3], out int d3))    // BC1
        {
            // Count Rising Edges
            if (lastPHI2 == 0 && d0 == 1) countPHI2++;
            if (lastLE == 0 && d1 == 1) countLE++;
            if (lastBDIR == 0 && d2 == 1) countBDIR++;
            if (lastBC1 == 0 && d3 == 1) countBC1++;
            
            // Timing Check: LE should only fall when PHI2 falls
            if (lastLE == 1 && d1 == 0) {
               if (d0 == 1) {
                   Console.WriteLine($"[WARN] YM_LE (Latch Enable) fell while PHI2 was still high! (Sample {samplesProcessed})");
               }
            }

            // Timing Check: BDIR should only fall when PHI2 falls
            if (lastBDIR == 1 && d2 == 0) {
               if (d0 == 1) {
                   Console.WriteLine($"[WARN] BDIR (Bus Direction) fell while PHI2 was still high! (Sample {samplesProcessed})");
               }
            }

            // Timing Check: BC1 should only fall when PHI2 falls
            if (lastBC1 == 1 && d3 == 0) {
               if (d0 == 1) {
                   Console.WriteLine($"[WARN] BC1 (Bus Control 1) fell while PHI2 was still high! (Sample {samplesProcessed})");
               }
            }

            // Timing Check: BDIR/BC1 should be stable throughout LE high
            if (d1 == 1 && (lastBDIR != d2 || lastBC1 != d3)) {
               // Console.WriteLine($"[INFO] BDIR/BC1 stable during LE pulse.");
            }

            lastPHI2 = d0; lastLE = d1; lastBDIR = d2; lastBC1 = d3;
            samplesProcessed++;
        }
    };

    Console.WriteLine("Capturing for 2.5 seconds at 2MHz...");
    process.Start();
    process.BeginOutputReadLine();
    process.WaitForExit();

    Console.WriteLine($"\n--- Latch Timing Report ---");
    Console.WriteLine($"Samples Analyzed: {samplesProcessed:N0}");
    Console.WriteLine($"PHI2 Pulses     : {countPHI2:N0}");
    Console.WriteLine($"YM_LE Pulses    : {countLE:N0}");
    Console.WriteLine($"BDIR Transitions: {countBDIR:N0}");
    Console.WriteLine($"BC1 Transitions : {countBC1:N0}");

    if (countLE == 0) {
        Console.WriteLine("\n[FAILURE] No YM_LE pulses detected. The Latch is never opening!");
        Console.WriteLine("Check Connection: GAL Pin 15 -> Latch Pin 11.");
    } else {
        Console.WriteLine("\n[SUCCESS] YM_LE is pulsing. If data is still stuck, check Latch Pin 1 (OE).");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
