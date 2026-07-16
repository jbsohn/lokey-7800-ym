using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Core;

public static class CommandLineUtils
{
    /// <summary>
    ///     Checks if a specific tool (like 7z) is available in the system's PATH.
    /// </summary>
    public static bool IsToolInstalled(string toolName)
    {
        try
        {
            var searchCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            using var p = Process.Start(new ProcessStartInfo(searchCmd, toolName)
            { RedirectStandardOutput = true, UseShellExecute = false });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
