using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using GameGuard.Models;

namespace GameGuard.Services;

public record ProcessActionInfo(string ProcessName, int Pid, double MemoryMB, string Reason, DateTime Timestamp);

public sealed class ProcessGuardian : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength,
        out int returnLength);

    private readonly GameGuardConfig _config;
    private ManagementEventWatcher? _wmiWatcher;
    private System.Threading.Timer? _differentialTimer;
    private readonly ConcurrentDictionary<int, string> _authorizedGamePids = new();
    private readonly ConcurrentDictionary<int, byte> _knownAllowedPids = new();
    private int _totalKilledCount;
    private int _totalBlockedLiveCount;
    private double _totalMemoryFreedByKillsMB;
    private DateTime _lastSteamLiteTick = DateTime.MinValue;

    public bool IsWatching { get; private set; }
    public int TotalKilledCount => _totalKilledCount;
    public int TotalBlockedLiveCount => _totalBlockedLiveCount;
    public double TotalMemoryFreedByKillsMB => Math.Round(_totalMemoryFreedByKillsMB, 1);
    public IReadOnlyDictionary<int, string> AuthorizedGames => _authorizedGamePids;

    public event Action<string>? OnLog;
    public event Action<ProcessActionInfo>? OnProcessTerminated;
    public event Action<string, int>? OnGameDetected;

    public ProcessGuardian(GameGuardConfig config)
    {
        _config = config;
    }

    public static int GetParentProcessId(Process process)
    {
        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int status = NtQueryInformationProcess(process.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status == 0)
            {
                return pbi.InheritedFromUniqueProcessId.ToInt32();
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Performs the initial sweep of running processes when Game Mode is activated.
    /// </summary>
    public async Task<(int KilledCount, double FreedMB)> ExecuteInitialPurgeAsync()
    {
        return await Task.Run(() =>
        {
            _knownAllowedPids.Clear();
            RefreshLauncherAndGameTree();

            int killed = 0;
            double freedMB = 0;

            var allProcesses = Process.GetProcesses();
            foreach (var proc in allProcesses)
            {
                using (proc)
                {
                    try
                    {
                        string name = NormalizeName(proc.ProcessName);
                        int pid = proc.Id;
                        int parentPid = GetParentProcessId(proc);
                        string? exePath = TryGetProcessPath(proc);

                        if (EvaluateProcess(name, pid, parentPid, exePath, out string reason))
                        {
                            double memMB = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
                            proc.Kill(entireProcessTree: false);
                            killed++;
                            freedMB += memMB;
                            Interlocked.Increment(ref _totalKilledCount);
                            _totalMemoryFreedByKillsMB += memMB;

                            var info = new ProcessActionInfo(name, pid, memMB, reason, DateTime.Now);
                            OnLog?.Invoke($"🛑 Cerrado: {name}.exe (PID {pid}, {memMB} MB) — {reason}");
                            OnProcessTerminated?.Invoke(info);
                        }
                        else
                        {
                            _knownAllowedPids.TryAdd(pid, 1);
                        }
                    }
                    catch
                    {
                        // Process exited or protected
                    }
                }
            }

            return (killed, Math.Round(freedMB, 1));
        });
    }

    /// <summary>
    /// Starts real-time process creation blocking (WMI Win32_ProcessStartTrace + fast differential fallback).
    /// </summary>
    public void StartRealTimeGuardian()
    {
        if (IsWatching)
            return;

        IsWatching = true;
        _lastSteamLiteTick = DateTime.UtcNow;

        // Layer 1: WMI Kernel Process Creation Trace (instant millisecond interception when elevated)
        try
        {
            var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
            _wmiWatcher = new ManagementEventWatcher(query);
            _wmiWatcher.EventArrived += OnWmiProcessStarted;
            _wmiWatcher.Start();
            OnLog?.Invoke("🛡️ Guardián WMI en tiempo real activo (Win32_ProcessStartTrace).");
        }
        catch
        {
            OnLog?.Invoke("ℹ️ Guardián activo en modo diferencial rápido (ejecuta como Admin para trazas WMI de kernel).");
        }

        // Layer 2: Differential backup & Steam child priority maintainer (every 1.5s, near-zero CPU)
        _differentialTimer = new System.Threading.Timer(OnDifferentialTick, null, 1500, 1500);
    }

    public void StopRealTimeGuardian()
    {
        IsWatching = false;

        if (_wmiWatcher != null)
        {
            try
            {
                _wmiWatcher.EventArrived -= OnWmiProcessStarted;
                _wmiWatcher.Stop();
                _wmiWatcher.Dispose();
            }
            catch { }
            _wmiWatcher = null;
        }

        if (_differentialTimer != null)
        {
            _differentialTimer.Dispose();
            _differentialTimer = null;
        }

        _authorizedGamePids.Clear();
        _knownAllowedPids.Clear();
        OnLog?.Invoke("⏹️ Guardián en tiempo real desactivado.");
    }

    private void OnWmiProcessStarted(object sender, EventArrivedEventArgs e)
    {
        if (!IsWatching)
            return;

        try
        {
            string rawName = e.NewEvent.Properties["ProcessName"]?.Value?.ToString() ?? "";
            int pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"]?.Value ?? 0);
            int parentPid = Convert.ToInt32(e.NewEvent.Properties["ParentProcessID"]?.Value ?? 0);

            if (pid <= 0)
                return;

            InspectNewProcess(pid, NormalizeName(rawName), parentPid);
        }
        catch { }
    }

    private void OnDifferentialTick(object? state)
    {
        if (!IsWatching)
            return;

        try
        {
            // Clean up exited game PIDs
            foreach (var gamePid in _authorizedGamePids.Keys.ToList())
            {
                try
                {
                    using var p = Process.GetProcessById(gamePid);
                    if (p.HasExited)
                        _authorizedGamePids.TryRemove(gamePid, out _);
                }
                catch
                {
                    _authorizedGamePids.TryRemove(gamePid, out _);
                }
            }

            // Check any newly spawned PIDs not yet inspected
            var currentProcs = Process.GetProcesses();
            foreach (var proc in currentProcs)
            {
                using (proc)
                {
                    int pid = proc.Id;
                    if (_knownAllowedPids.ContainsKey(pid))
                        continue;

                    string name = NormalizeName(proc.ProcessName);
                    int parentPid = GetParentProcessId(proc);
                    InspectNewProcess(pid, name, parentPid, proc);
                }
            }

            // Periodically re-trim steamwebhelper every 30s if Steam Lite Mode is enabled
            if (_config.SteamLiteMode && (DateTime.UtcNow - _lastSteamLiteTick).TotalSeconds >= 30)
            {
                _lastSteamLiteTick = DateTime.UtcNow;
                MemoryOptimizer.ApplySteamLiteMode();
            }
        }
        catch { }
    }

    private void InspectNewProcess(int pid, string normalizedName, int parentPid, Process? existingProc = null)
    {
        if (_knownAllowedPids.ContainsKey(pid))
            return;

        Process? proc = existingProc;
        bool disposeProc = false;
        try
        {
            if (proc == null)
            {
                proc = Process.GetProcessById(pid);
                disposeProc = true;
            }

            if (string.IsNullOrEmpty(normalizedName))
                normalizedName = NormalizeName(proc.ProcessName);

            if (parentPid <= 0)
                parentPid = GetParentProcessId(proc);

            string? exePath = TryGetProcessPath(proc);

            if (EvaluateProcess(normalizedName, pid, parentPid, exePath, out string reason))
            {
                double memMB = 0;
                try { memMB = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1); } catch { }

                proc.Kill(entireProcessTree: true);
                Interlocked.Increment(ref _totalKilledCount);
                Interlocked.Increment(ref _totalBlockedLiveCount);
                _totalMemoryFreedByKillsMB += memMB;

                var info = new ProcessActionInfo(normalizedName, pid, memMB, $"Bloqueado en vivo ({reason})", DateTime.Now);
                OnLog?.Invoke($"🚫 BLOQUEADO EN VIVO: {normalizedName}.exe (PID {pid}) — {reason}");
                OnProcessTerminated?.Invoke(info);
            }
            else
            {
                _knownAllowedPids.TryAdd(pid, 1);
            }
        }
        catch
        {
            // Process already exited
        }
        finally
        {
            if (disposeProc && proc != null)
                proc.Dispose();
        }
    }

    /// <summary>
    /// Returns true if the process should be terminated, or false if it is allowed.
    /// Also detects and boosts Steam/Launcher child games automatically.
    /// </summary>
    public bool EvaluateProcess(string name, int pid, int parentPid, string? exePath, out string reason)
    {
        reason = string.Empty;

        if (pid <= 4 || pid == Environment.ProcessId)
            return false;

        // 1. Absolute Kernel & OS Session Safe-List
        if (GameGuardConfig.KernelSafeList.Contains(name))
            return false;

        // 2. Explicit Blocked List (Always kill bloatware/telemetry/updaters)
        if (_config.ExplicitBlockedProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            reason = "Lista Negra / Bloatware";
            return true;
        }

        // 4. Smart Steam / Launcher Child & Game Directory Detection
        if (_config.AutoAuthorizeSteamChildren && IsChildOfLauncherOrGame(pid, parentPid, name, exePath))
        {
            RegisterAndBoostGameProcess(pid, name);
            return false;
        }

        // 5. User & Hardware Whitelist (plus UnrealCEFSubProcess_* and BEService_* prefixes)
        if (_config.WhitelistedProcesses.Contains(name, StringComparer.OrdinalIgnoreCase) ||
            name.StartsWith("UnrealCEFSubProcess", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("BEService", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 6. Protect native Windows system binaries in System32 / SysWOW64 (excluding DriverStore bloatware)
        if (!string.IsNullOrEmpty(exePath))
        {
            bool isWindowsCoreDir =
                (exePath.StartsWith(@"C:\Windows\System32\", StringComparison.OrdinalIgnoreCase) ||
                 exePath.StartsWith(@"C:\Windows\SysWOW64\", StringComparison.OrdinalIgnoreCase) ||
                 exePath.StartsWith(@"C:\Windows\WinSxS\", StringComparison.OrdinalIgnoreCase)) &&
                !exePath.Contains(@"\DriverStore\", StringComparison.OrdinalIgnoreCase);

            if (isWindowsCoreDir)
                return false;
        }
        else
        {
            // If we cannot read the path of a process, it is a protected SYSTEM service process; do not kill blindly unless in ExplicitBlockedProcesses
            return false;
        }

        // 7. Strict User Whitelist Mode: Kill anything else not on the Whitelist
        if (_config.StrictUserWhitelist)
        {
            reason = "No está en Lista Blanca";
            return true;
        }

        return false;
    }

    public void RefreshLauncherAndGameTree()
    {
        if (!_config.AutoAuthorizeSteamChildren)
            return;

        var procs = Process.GetProcesses();
        var launcherPids = new HashSet<int>();

        foreach (var p in procs)
        {
            try
            {
                string n = NormalizeName(p.ProcessName);
                if (_config.TrustedLaunchers.Contains(n, StringComparer.OrdinalIgnoreCase))
                {
                    launcherPids.Add(p.Id);
                }
            }
            catch { }
        }

        foreach (var p in procs)
        {
            try
            {
                int parentId = GetParentProcessId(p);
                string n = NormalizeName(p.ProcessName);
                string? path = TryGetProcessPath(p);

                if (IsChildOfLauncherOrGame(p.Id, parentId, n, path) ||
                    (launcherPids.Contains(parentId) &&
                     !_config.WhitelistedProcesses.Contains(n, StringComparer.OrdinalIgnoreCase) &&
                     !GameGuardConfig.KernelSafeList.Contains(n)))
                {
                    RegisterAndBoostGameProcess(p.Id, n);
                }
            }
            catch { }
            finally
            {
                p.Dispose();
            }
        }
    }

    private bool IsChildOfLauncherOrGame(int pid, int parentPid, string processName, string? exePath = null)
    {
        if (_authorizedGamePids.ContainsKey(pid))
            return true;

        // Ignore Steam internal web helper and overlay processes so they don't get High CPU priority
        if (processName.Equals("steamwebhelper", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("steamservice", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("GameOverlayUI", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("gameoverlayui64", StringComparison.OrdinalIgnoreCase) ||
            processName.StartsWith("BEService", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("zksvc", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Known primary game executables (e.g. PUBG TslGame) or executables inside Steam/Epic game folders
        if (processName.Equals("TslGame", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("TslGame_BE", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("TslGame_ZK", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("ExecPubg", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(exePath) &&
             (exePath.Contains(@"\steamapps\common\", StringComparison.OrdinalIgnoreCase) ||
              exePath.Contains(@"\Epic Games\", StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        if (parentPid <= 4)
            return false;

        if (_authorizedGamePids.ContainsKey(parentPid))
            return true;

        try
        {
            using var parent = Process.GetProcessById(parentPid);
            string parentName = NormalizeName(parent.ProcessName);
            if (_config.TrustedLaunchers.Contains(parentName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    private void RegisterAndBoostGameProcess(int pid, string gameName)
    {
        if (_authorizedGamePids.TryAdd(pid, gameName))
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                if (_config.BoostGameCpuPriority)
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                }
                OnLog?.Invoke($"🎮 Juego detectado (Hijo de Launcher): {gameName}.exe (PID {pid}) -> Autorizado y Prioridad ALTA");
                OnGameDetected?.Invoke(gameName, pid);
            }
            catch { }
        }
    }

    public static string? TryGetProcessPath(Process proc)
    {
        try
        {
            return proc.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    public static string NormalizeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        string trimmed = rawName.Trim();
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return trimmed[..^4];

        return trimmed;
    }

    public void Dispose()
    {
        StopRealTimeGuardian();
    }
}
