using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GameGuard.Services;

public sealed class PowerPlanManager
{
    private string? _previousSchemeGuid;

    public static (string Guid, string Name) GetActivePlan()
    {
        try
        {
            var psi = new ProcessStartInfo("powercfg", "/getactivescheme")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);
                var match = Regex.Match(output, @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\s*\((.+?)\)");
                if (match.Success)
                {
                    return (match.Groups[1].Value, match.Groups[2].Value.Trim());
                }
            }
        }
        catch { }

        return ("", "Desconocido");
    }

    public bool ActivateGamePowerPlan(string gamePlanGuid)
    {
        var current = GetActivePlan();
        if (!string.IsNullOrEmpty(current.Guid))
        {
            _previousSchemeGuid = current.Guid;
        }

        return SetActivePlan(gamePlanGuid);
    }

    public bool RestorePowerPlan(string fallbackBalancedGuid)
    {
        string target = !string.IsNullOrEmpty(_previousSchemeGuid) &&
                        !string.Equals(_previousSchemeGuid, "d5071f6b-3d62-442f-9b94-acae1129cdc4", StringComparison.OrdinalIgnoreCase)
            ? _previousSchemeGuid
            : fallbackBalancedGuid;

        return SetActivePlan(target);
    }

    public static bool SetActivePlan(string schemeGuid)
    {
        if (string.IsNullOrWhiteSpace(schemeGuid))
            return false;

        try
        {
            var psi = new ProcessStartInfo("powercfg", $"/setactive {schemeGuid}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(2000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
