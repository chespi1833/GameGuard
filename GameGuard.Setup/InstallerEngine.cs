using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace GameGuard.Setup;

public sealed class InstallOptions
{
    public string InstallDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "GameGuard");

    public bool CreatePublicDesktopShortcut { get; set; } = true;
    public bool CreateAllUsersStartMenuShortcut { get; set; } = true;
    public bool ConfigureSharedProgramData { get; set; } = true;
    public bool RegisterInWindowsUninstall { get; set; } = true;
    public bool LaunchAfterInstall { get; set; } = true;
}

public static class InstallerEngine
{
    private const string UninstallRegKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GameGuard";

    public static void Install(InstallOptions options, Action<string, int>? onProgress = null)
    {
        onProgress?.Invoke("Deteniendo instancias activas de GameGuard...", 10);
        StopRunningInstances();

        onProgress?.Invoke($"Creando directorio de instalación: {options.InstallDir}...", 25);
        Directory.CreateDirectory(options.InstallDir);

        onProgress?.Invoke("Extrayendo archivos del programa...", 45);
        ExtractPayloadTo(options.InstallDir);

        // Copy the setup binary itself as Uninstaller
        string setupTarget = Path.Combine(options.InstallDir, "GameGuard_Setup.exe");
        string? currentExe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(currentExe) &&
            !string.Equals(Path.GetFullPath(currentExe), Path.GetFullPath(setupTarget), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Copy(currentExe, setupTarget, overwrite: true);
            }
            catch { }
        }

        if (options.ConfigureSharedProgramData)
        {
            onProgress?.Invoke("Configurando carpeta compartida para todos los usuarios (C:\\ProgramData\\GameGuard)...", 65);
            ConfigureSharedConfigDirectory(options.InstallDir);
        }

        string installedExe = Path.Combine(options.InstallDir, "GameGuard.exe");

        if (options.CreatePublicDesktopShortcut)
        {
            onProgress?.Invoke("Creando acceso directo en el Escritorio Público (Todos los usuarios)...", 75);
            string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            CreateShortcut(Path.Combine(publicDesktop, "GameGuard.lnk"), installedExe, options.InstallDir);

            // Clean up legacy shortcuts if present
            string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            foreach (string legacyName in new[] { "GameGuard.lnk", "OMEN GameGuard.lnk" })
            {
                string userLnk = Path.Combine(userDesktop, legacyName);
                if (File.Exists(userLnk) && !string.Equals(publicDesktop, userDesktop, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(userLnk); } catch { }
                }
            }
        }

        if (options.CreateAllUsersStartMenuShortcut)
        {
            onProgress?.Invoke("Creando acceso directo en el Menú Inicio para todos los usuarios...", 85);
            string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            CreateShortcut(Path.Combine(commonStartMenu, "GameGuard.lnk"), installedExe, options.InstallDir);
        }

        if (options.RegisterInWindowsUninstall)
        {
            onProgress?.Invoke("Registrando en Aplicaciones Instaladas de Windows...", 95);
            RegisterUninstaller(options.InstallDir, installedExe, setupTarget);
        }

        onProgress?.Invoke("✅ ¡Instalación completada con éxito para todos los usuarios!", 100);
    }

    public static void Uninstall(string? installDir = null, Action<string, int>? onProgress = null)
    {
        installDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "GameGuard");

        onProgress?.Invoke("Deteniendo instancias activas de GameGuard...", 15);
        StopRunningInstances();

        onProgress?.Invoke("Eliminando accesos directos de Escritorio Público y Menú Inicio...", 45);
        try
        {
            string publicDesktopLnk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                "GameGuard.lnk");
            if (File.Exists(publicDesktopLnk)) File.Delete(publicDesktopLnk);

            string userDesktopLnk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "GameGuard.lnk");
            if (File.Exists(userDesktopLnk)) File.Delete(userDesktopLnk);

            string startMenuLnk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                "GameGuard.lnk");
            if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);
        }
        catch { }

        onProgress?.Invoke("Eliminando registro de Windows...", 70);
        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(UninstallRegKeyPath, throwOnMissingSubKey: false);
        }
        catch { }

        onProgress?.Invoke("Eliminando archivos del programa...", 90);
        try
        {
            if (Directory.Exists(installDir))
            {
                foreach (string file in Directory.GetFiles(installDir))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { }

        onProgress?.Invoke("✅ GameGuard ha sido desinstalado.", 100);
    }

    private static void StopRunningInstances()
    {
        foreach (string procName in new[] { "GameGuard", "OmenGameGuard" })
        {
            foreach (var proc in Process.GetProcessesByName(procName))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
                catch { }
                finally
                {
                    proc.Dispose();
                }
            }
        }
    }

    private static void ExtractPayloadTo(string targetDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        string? resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

        if (resName == null)
        {
            throw new InvalidOperationException("No se encontró el paquete embebido payload.zip dentro del instalador.");
        }

        using Stream? stream = asm.GetManifestResourceStream(resName);
        if (stream == null)
            throw new InvalidOperationException("No se pudo abrir payload.zip.");

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            string destPath = Path.Combine(targetDir, entry.FullName);
            string? destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            entry.ExtractToFile(destPath, overwrite: true);
        }
    }

    private static void ConfigureSharedConfigDirectory(string installDir)
    {
        string commonDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GameGuard");
        Directory.CreateDirectory(commonDir);

        string seedConfig = Path.Combine(installDir, "config.json");
        string sharedConfig = Path.Combine(commonDir, "config.json");
        if (File.Exists(seedConfig) && !File.Exists(sharedConfig))
        {
            try { File.Copy(seedConfig, sharedConfig, overwrite: false); } catch { }
        }

        // Grant Modify permission to BUILTIN\Users (*S-1-5-32-545) so any user on the PC can save their Whitelist
        try
        {
            var psi = new ProcessStartInfo("icacls", $"\"{commonDir}\" /grant *S-1-5-32-545:(OI)(CI)M /T /C")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch { }
    }

    private static void CreateShortcut(string shortcutPath, string targetExe, string workingDir)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                return;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null)
                return;

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExe;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = "GameGuard - Guardián de Modo Juego y Lista Blanca";
            shortcut.IconLocation = $"{targetExe},0";
            shortcut.Save();
        }
        catch { }
    }

    private static void RegisterUninstaller(string installDir, string installedExe, string setupExe)
    {
        try
        {
            using RegistryKey key = Registry.LocalMachine.CreateSubKey(UninstallRegKeyPath);
            key.SetValue("DisplayName", "GameGuard (Modo Juego y Lista Blanca)");
            key.SetValue("DisplayIcon", installedExe);
            key.SetValue("DisplayVersion", "1.0.0");
            key.SetValue("Publisher", "GameGuard Open Source Contributors");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("UninstallString", $"\"{setupExe}\" --uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 0, RegistryValueKind.DWord);
            key.SetValue("EstimatedSize", 2560, RegistryValueKind.DWord);
        }
        catch { }
    }
}
