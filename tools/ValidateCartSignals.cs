#!/usr/bin/env dotnet-script
using System;
using System.Diagnostics;

// Sigrok Capture Script (C# 10 Top-Level Statements)
// Captures pins 0-3 which correspond to:
// Pin 0 (D0) = Clock (PHI2)
// Pin 1 (D1) = R/W
// Pin 2 (D2) = Halt
// Pin 3 (D3) = A15

Console.WriteLine("Starting Sigrok capture for pins 0-3...");
Console.WriteLine("Mapping: D0=Clock, D1=R/W, D2=Halt, D3=A15");
Console.WriteLine("-------------------------------------------------");

var psi = new ProcessStartInfo
{
    FileName = "sigrok-cli",
    // Adjust driver if you are using something other than a generic Cypress FX2 logic analyzer
    Arguments = "--driver fx2lafw --config samplerate=24M --samples 100000 --channels D0,D1,D2,D3 -O csv",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

int countClock = 0, countRW = 0, countHalt = 0, countA15 = 0;
int lastClock = -1, lastRW = -1, lastHalt = -1, lastA15 = -1;
int samplesProcessed = 0;

try 
{
    using var process = new Process { StartInfo = psi };
    process.OutputDataReceived += (s, e) => {
        if (string.IsNullOrWhiteSpace(e.Data)) return;
        var line = e.Data.Trim();
        if (line.StartsWith("D") || line.StartsWith("l")) return; // Skip headers

        var parts = line.Split(',');
        if (parts.Length >= 4 && 
            int.TryParse(parts[0], out int d0) &&
            int.TryParse(parts[1], out int d1) &&
            int.TryParse(parts[2], out int d2) &&
            int.TryParse(parts[3], out int d3))
        {
            if (lastClock != -1 && d0 == 1 && lastClock == 0) countClock++;
            if (lastRW != -1 && d1 == 1 && lastRW == 0) countRW++;
            if (lastHalt != -1 && d2 == 1 && lastHalt == 0) countHalt++;
            if (lastA15 != -1 && d3 == 1 && lastA15 == 0) countA15++;

            lastClock = d0;
            lastRW = d1;
            lastHalt = d2;
            lastA15 = d3;
            samplesProcessed++;
        }
    };
    process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine($"[error] {e.Data}"); };

    Console.WriteLine("Capturing and analyzing stream (this may take a moment)...");
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    Console.WriteLine($"\nCapture finished with exit code {process.ExitCode}");
    Console.WriteLine($"\n--- Transition Report (Low-to-High Edges) ---");
    Console.WriteLine($"Samples Analyzed: {samplesProcessed:N0}");
    Console.WriteLine($"Clock (PHI2) : {countClock:N0} transitions");
    Console.WriteLine($"R/W          : {countRW:N0} transitions");
    Console.WriteLine($"Halt         : {countHalt:N0} transitions");
    Console.WriteLine($"A15          : {countA15:N0} transitions");
}
catch (Exception ex)
{
    Console.WriteLine($"\nFailed to start sigrok-cli. Is it installed and in your PATH?\nError: {ex.Message}");
}
