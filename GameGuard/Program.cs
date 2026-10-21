using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using GameGuard.Models;
using GameGuard.Services;

namespace GameGuard;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Support CLI dry-run test flag for automated verification
        if (args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase))
        {
            RunCliDryRun();
            return;
        }

        // Prevent duplicate instances
        using var mutex = new Mutex(true, "Global\\GameGuard_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "GameGuard ya está ejecutándose (revisa la bandeja del sistema junto al reloj).",
                "GameGuard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Auto-elevate to Administrator unless --no-elevate is passed
        if (!args.Contains("--no-elevate", StringComparer.OrdinalIgnoreCase) && !IsAdmin())
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var psi = new ProcessStartInfo(exePath)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = "--no-elevate"
                    };
                    Process.Start(psi);
                    return;
                }
            }
            catch
            {
                // User declined UAC prompt; continue launching in standard mode
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void RunCliDryRun()
    {
        var config = GameGuardConfig.Load();
        using var guardian = new ProcessGuardian(config);
        var mem = MemoryOptimizer.GetMemorySnapshot();

        Console.WriteLine($"[DryRun] RAM Libre actual: {mem.AvailableMB:N1} MB / {mem.TotalMB:N1} MB ({mem.UsedPercent}% en uso)");

        int killCount = 0;
        double killMB = 0;

        foreach (var proc in Process.GetProcesses())
        {
            using (proc)
            {
                try
                {
                    string name = ProcessGuardian.NormalizeName(proc.ProcessName);
                    int pid = proc.Id;
                    int parentPid = ProcessGuardian.GetParentProcessId(proc);
                    string? path = ProcessGuardian.TryGetProcessPath(proc);

                    if (guardian.EvaluateProcess(name, pid, parentPid, path, out string reason))
                    {
                        double mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
                        killCount++;
                        killMB += mb;
                        Console.WriteLine($"  -> [Cerrar] {name}.exe (PID {pid}, {mb} MB) | Motivo: {reason}");
                    }
                }
                catch { }
            }
        }

        int svcCount = 0;
        foreach (string svc in config.ServicesToPause.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var sc = new ServiceController(svc);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    svcCount++;
                    Console.WriteLine($"  -> [Pausar Servicio] {sc.DisplayName} ({svc})");
                }
            }
            catch { }
        }

        Console.WriteLine($"[DryRun Resumen] Procesos a cerrar: {killCount} (~{killMB:N1} MB) | Servicios activos a pausar: {svcCount}");
    }
}
