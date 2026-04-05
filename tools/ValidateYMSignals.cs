#!/usr/bin/env dotnet-script
using System;
using System.Collections.Generic;
using System.Diagnostics;

// YM2149 Signal Diagnostic Script (V1.4)
// Standardized format "like ValidateCartSignals"
//
// Mapped to Physical YM2149 pins:
// D0 (Sigrok) = Pin 22 (CLK)
// D1 (Sigrok) = Pin 27 (BDIR)
// D2 (Sigrok) = Pin 29 (BC1)
// D3 (Sigrok) = Pin 33 (DA4)
// D4 (Sigrok) = Pin 34 (DA3)
// D5 (Sigrok) = Pin 35 (DA2)
// D6 (Sigrok) = Pin 36 (DA1)
// D7 (Sigrok) = Pin 37 (DA0)

Console.WriteLine("Starting Sigrok capture for YM2149 Header (8 Pins)...");
Console.WriteLine("Mapping: D0=CLK, D1=BDIR, D2=BC1, D3=DA4, D4=DA3, D5=DA2, D6=DA1, D7=DA0");
Console.WriteLine("-------------------------------------------------");

var psi = new ProcessStartInfo
{
    FileName = "sigrok-cli",
    Arguments = "--driver fx2lafw --config samplerate=1M --samples 5000000 --channels D0,D1,D2,D3,D4,D5,D6,D7 -O csv",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

int countCLK = 0, countBDIR = 0, countBC1 = 0;
int countD0 = 0, countD1 = 0, countD2 = 0, countD3 = 0, countD4 = 0;
int lastCLK = -1, lastBDIR = -1, lastBC1 = -1;
int lastD0 = -1, lastD1 = -1, lastD2 = -1, lastD3 = -1, lastD4 = -1;
int samplesProcessed = 0;

// Register names for decoding
var regNames = new Dictionary<int, string> {
    { 0, "ChA Fine" }, { 1, "ChA Coarse" }, { 2, "ChB Fine" }, { 3, "ChB Coarse" },
    { 4, "ChC Fine" }, { 5, "ChC Coarse" }, { 6, "Noise Per" }, { 7, "Mixer Control" },
    { 8, "Amp A" }, { 9, "Amp B" }, { 10, "Amp C" }, { 11, "Env Low" },
    { 12, "Env High" }, { 13, "Env Shape" }, { 14, "Port A" }, { 15, "Port B" }
};

try 
{
    using var process = new Process { StartInfo = psi };
    process.OutputDataReceived += (s, e) => {
        if (string.IsNullOrWhiteSpace(e.Data)) return;
        var line = e.Data.Trim();
        if (line.StartsWith("D") || line.StartsWith("l")) return; // Skip headers

        var parts = line.Split(',');
        if (parts.Length >= 8 && 
            int.TryParse(parts[0], out int d0) &&  // CLK
            int.TryParse(parts[1], out int d1) &&  // BDIR
            int.TryParse(parts[2], out int d2) &&  // BC1
            int.TryParse(parts[3], out int d3) &&  // DA4
            int.TryParse(parts[4], out int d4) &&  // DA3
            int.TryParse(parts[5], out int d5) &&  // DA2
            int.TryParse(parts[6], out int d6) &&  // DA1
            int.TryParse(parts[7], out int d7))    // DA0
        {
            // Transition Counting (Standard format)
            if (lastCLK != -1 && d0 == 1 && lastCLK == 0) countCLK++;
            if (lastBDIR != -1 && d1 == 1 && lastBDIR == 0) countBDIR++;
            if (lastBC1 != -1 && d2 == 1 && lastBC1 == 0) countBC1++;
            
            // Data Bits
            if (lastD4 != -1 && d4 == 1 && lastD4 == 0) countD0++;
            if (lastD3 != -1 && d3 == 1 && lastD3 == 0) countD1++;

            // Real-time Decoder: Look for Latch Address (BDIR=1, BC1=1)
            if (d1 == 1 && d2 == 1 && (lastBDIR == 0 || lastBC1 == 0)) {
                int regIdx = (d4 << 3) | (d5 << 2) | (d6 << 1) | d7;
                string name = regNames.ContainsKey(regIdx) ? regNames[regIdx] : "Unknown";
                Console.WriteLine($"[LATCH] Register {regIdx:X} ({name})");
            }
            
            // Real-time Decoder: Look for Write Data (BDIR=1, BC1=0)
            if (d1 == 1 && d2 == 0 && (lastBDIR == 0 || lastBC1 == 1)) {
                int val = (d3 << 4) | (d4 << 3) | (d5 << 2) | (d6 << 1) | d7;
                Console.WriteLine($"[WRITE] Value: ${val:X2} (%{Convert.ToString(val, 2).PadLeft(5, '0')})");
            }

            lastCLK = d0; lastBDIR = d1; lastBC1 = d2;
            lastD3 = d3; lastD4 = d4;
            samplesProcessed++;
        }
    };
    process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine($"[error] {e.Data}"); };

    Console.WriteLine("Capturing for 5 seconds (be patient)...");
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    Console.WriteLine($"\n--- Transition Report (YM2149 Header) ---");
    Console.WriteLine($"Samples Analyzed: {samplesProcessed:N0}");
    Console.WriteLine($"Clock In (Pin 22) : {countCLK:N0} transitions");
    Console.WriteLine($"BDIR     (Pin 27) : {countBDIR:N0} transitions");
    Console.WriteLine($"BC1      (Pin 29) : {countBC1:N0} transitions");
    Console.WriteLine($"Data Bits(DA0-DA4): Active");
}
catch (Exception ex)
{
    Console.WriteLine($"\nFailed to start diagnostic.\nError: {ex.Message}");
}
