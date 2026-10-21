using System.ServiceProcess;

namespace GameGuard.Services;

public sealed class WindowsServiceManager
{
    private readonly HashSet<string> _servicesStoppedBySession = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> ServicesStoppedBySession => _servicesStoppedBySession;

    public async Task<List<(string ServiceName, string DisplayName, bool Success, string Message)>> PauseServicesAsync(
        IEnumerable<string> serviceNames,
        Action<string>? onLog = null)
    {
        var results = new List<(string ServiceName, string DisplayName, bool Success, string Message)>();
        var names = serviceNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        await Task.Run(() =>
        {
            foreach (string name in names)
            {
                try
                {
                    using var sc = new ServiceController(name);
                    string displayName = sc.DisplayName;

                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        if (sc.CanStop)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(4));
                            _servicesStoppedBySession.Add(name);
                            results.Add((name, displayName, true, "Detenido temporalmente"));
                            onLog?.Invoke($"⏸️ Servicio pausado: {displayName} ({name})");
                        }
                        else
                        {
                            results.Add((name, displayName, false, "No permite detenerse"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Service might not exist on this machine or requires higher privileges
                    results.Add((name, name, false, ex.Message));
                }
            }
        });

        return results;
    }

    public async Task<int> RestoreServicesAsync(Action<string>? onLog = null)
    {
        int restored = 0;
        var toRestore = _servicesStoppedBySession.ToList();

        await Task.Run(() =>
        {
            foreach (string name in toRestore)
            {
                try
                {
                    using var sc = new ServiceController(name);
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(4));
                        restored++;
                        onLog?.Invoke($"▶️ Servicio restaurado: {sc.DisplayName} ({name})");
                    }
                    _servicesStoppedBySession.Remove(name);
                }
                catch
                {
                    // Ignore if service fails to restart immediately
                }
            }
        });

        return restored;
    }
}
