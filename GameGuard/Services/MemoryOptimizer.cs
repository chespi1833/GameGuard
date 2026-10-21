using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameGuard.Services;

public record MemorySnapshot(double TotalMB, double AvailableMB, double UsedMB, int UsedPercent);

public static class MemoryOptimizer
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const int SystemMemoryListInformation = 80;
    private const int MemoryPurgeStandbyList = 4;
    private const int MemoryFlushModifiedList = 3;
    private const int MemoryEmptyWorkingSets = 2;

    public static MemorySnapshot GetMemorySnapshot()
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            double total = Math.Round(mem.ullTotalPhys / (1024.0 * 1024.0), 1);
            double avail = Math.Round(mem.ullAvailPhys / (1024.0 * 1024.0), 1);
            double used = Math.Round(total - avail, 1);
            return new MemorySnapshot(total, avail, used, (int)mem.dwMemoryLoad);
        }
        return new MemorySnapshot(0, 0, 0, 0);
    }

    /// <summary>
    /// Purges Working Sets of all accessible processes and flushes Windows Standby List (MemReduct style).
    /// Returns the amount of RAM freed in MB.
    /// </summary>
    public static (double FreedMB, int TrimmedProcesses) PurgeRam(HashSet<int>? excludePids = null)
    {
        var before = GetMemorySnapshot();
        int trimmed = 0;

        foreach (var proc in Process.GetProcesses())
        {
            using (proc)
            {
                try
                {
                    if (excludePids != null && excludePids.Contains(proc.Id))
                        continue;

                    if (EmptyWorkingSet(proc.Handle))
                    {
                        trimmed++;
                    }
                }
                catch
                {
                    // Protected or inaccessible process
                }
            }
        }

        // Flush Windows Standby & Working Set lists at kernel level if running as Admin
        try
        {
            if (EnablePrivilege("SeProfileSingleProcessPrivilege"))
            {
                int cmdEmptyWs = MemoryEmptyWorkingSets;
                NtSetSystemInformation(SystemMemoryListInformation, ref cmdEmptyWs, sizeof(int));

                int cmdFlushMod = MemoryFlushModifiedList;
                NtSetSystemInformation(SystemMemoryListInformation, ref cmdFlushMod, sizeof(int));

                int cmdPurgeStandby = MemoryPurgeStandbyList;
                NtSetSystemInformation(SystemMemoryListInformation, ref cmdPurgeStandby, sizeof(int));
            }
        }
        catch
        {
            // Ignore if not elevated
        }

        var after = GetMemorySnapshot();
        double freed = Math.Max(0, Math.Round(after.AvailableMB - before.AvailableMB, 1));
        return (freed, trimmed);
    }

    /// <summary>
    /// "Steam Ligero": Lowers priority of all steamwebhelper.exe processes and trims their memory.
    /// Returns how much memory steamwebhelper was using before vs after.
    /// </summary>
    public static (double SavedMB, int HelperCount) ApplySteamLiteMode()
    {
        var helpers = Process.GetProcessesByName("steamwebhelper");
        if (helpers.Length == 0)
            return (0, 0);

        double beforeMB = 0;
        foreach (var p in helpers)
        {
            try
            {
                beforeMB += p.WorkingSet64 / (1024.0 * 1024.0);
                p.PriorityClass = ProcessPriorityClass.BelowNormal;
                EmptyWorkingSet(p.Handle);
            }
            catch
            {
                // Ignore if process exited
            }
            finally
            {
                p.Dispose();
            }
        }

        double afterMB = 0;
        var helpersAfter = Process.GetProcessesByName("steamwebhelper");
        foreach (var p in helpersAfter)
        {
            try
            {
                afterMB += p.WorkingSet64 / (1024.0 * 1024.0);
            }
            catch { }
            finally
            {
                p.Dispose();
            }
        }

        double saved = Math.Max(0, Math.Round(beforeMB - afterMB, 1));
        return (saved, helpers.Length);
    }

    public static void RestoreSteamNormalMode()
    {
        var helpers = Process.GetProcessesByName("steamwebhelper");
        foreach (var p in helpers)
        {
            try
            {
                p.PriorityClass = ProcessPriorityClass.Normal;
            }
            catch { }
            finally
            {
                p.Dispose();
            }
        }
    }

    private static bool EnablePrivilege(string privilegeName)
    {
        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
            return false;

        try
        {
            if (!LookupPrivilegeValue(null, privilegeName, out LUID luid))
                return false;

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            return AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }
}
