#!/usr/bin/env dotnet-script
using System;
using System.Diagnostics;

// Sigrok Capture Script (C# 10 Top-Level Statements)
// Captures pins 0-3 which correspond to GAL pins:
// Pin 0 (D0) = YM_LE (Pin 15)
// Pin 1 (D1) = PHI2OUT (Pin 16)
// Pin 2 (D2) = BC1 (Pin 17)
// Pin 3 (D3) = BDIR (Pin 18)

Console.WriteLine("Starting Sigrok capture for GAL pins 15-18...");
Console.WriteLine("Mapping: D0=YM_LE, D1=PHI2OUT, D2=BC1, D3=BDIR");
Console.WriteLine("-------------------------------------------------");

var psi = new ProcessStartInfo
{
    FileName = "sigrok-cli",
    // Adjust driver if you are using something other than a generic Cypress FX2 logic analyzer
    Arguments = "--driver fx2lafw --config samplerate=1M --samples 5000000 --channels D0,D1,D2,D3 -O csv",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

int countYMLE = 0, countPHI2OUT = 0, countBC1 = 0, countBDIR = 0;
int lastYMLE = -1, lastPHI2OUT = -1, lastBC1 = -1, lastBDIR = -1;
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
            if (lastYMLE != -1 && d0 == 1 && lastYMLE == 0) countYMLE++;
            if (lastPHI2OUT != -1 && d1 == 1 && lastPHI2OUT == 0) countPHI2OUT++;
            if (lastBC1 != -1 && d2 == 1 && lastBC1 == 0) countBC1++;
            if (lastBDIR != -1 && d3 == 1 && lastBDIR == 0) countBDIR++;

            lastYMLE = d0;
            lastPHI2OUT = d1;
            lastBC1 = d2;
            lastBDIR = d3;
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
    Console.WriteLine($"YM_LE   (Pin 15) : {countYMLE:N0} transitions");
    Console.WriteLine($"PHI2OUT (Pin 16) : {countPHI2OUT:N0} transitions");
    Console.WriteLine($"BC1     (Pin 17) : {countBC1:N0} transitions");
    Console.WriteLine($"BDIR    (Pin 18) : {countBDIR:N0} transitions");
}
catch (Exception ex)
{
    Console.WriteLine($"\nFailed to start sigrok-cli. Is it installed and in your PATH?\nError: {ex.Message}");
}
