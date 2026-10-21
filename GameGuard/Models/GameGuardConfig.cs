using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuard.Services;

namespace GameGuard.Models;

public sealed class GameGuardConfig
{
    public string LastDetectedMachineSignature { get; set; } = "";

    // Feature Toggles
    public bool AutoAuthorizeSteamChildren { get; set; } = true;
    public bool BoostGameCpuPriority { get; set; } = true;
    public bool DeepRamPurge { get; set; } = true;
    public bool SwitchPowerPlan { get; set; } = true;
    public bool SteamLiteMode { get; set; } = true;
    public bool StrictUserWhitelist { get; set; } = true;

    // Power Plans (Windows Ultimate Performance & Balanced GUIDs)
    public string GamePowerPlanGuid { get; set; } = "d5071f6b-3d62-442f-9b94-acae1129cdc4"; // Ultimate Performance
    public string BalancedPowerPlanGuid { get; set; } = "381b4222-f694-41f0-9685-ff5bb260df2e"; // Equilibrado

    // Game Launchers whose child processes are automatically trusted & boosted
    public List<string> TrustedLaunchers { get; set; } =
    [
        "steam",
        "EpicGamesLauncher",
        "Battle.net",
        "EADesktop",
        "UbisoftConnect",
        "GalaxyClient",
        "riotclientservices"
    ];

    // User & Hardware Whitelist (Allowed to stay open while gaming)
    public List<string> WhitelistedProcesses { get; set; } =
    [
        // Self
        "GameGuard",
        "GameGuard_Setup",

        // Steam Core & Overlay (32-bit and 64-bit)
        "steam",
        "steamservice",
        "steamwebhelper",
        "GameOverlayUI",
        "gameoverlayui64",
        "steamerrorreporter",
        "steamerrorreporter64",

        // PUBG (PlayerUnknown's Battlegrounds) & Anti-Cheat (BattlEye + Zakynthos)
        "TslGame",
        "TslGame_BE",
        "TslGame_ZK",
        "ExecPubg",
        "UnrealCEFSubProcess_3071",
        "UnrealCEFSubProcess",
        "iigw_server",
        "BroCrashReporter",
        "BEService",
        "BEService_pubg",
        "BEService_x64",
        "zksvc",
        "EasyAntiCheat",
        "EasyAntiCheat_EOS",

        // HP OMEN Thermal / Fan & Intel XTU Power Control
        "OmenCap",
        "XtuService",
        "esif_uf",

        // Logitech Peripherals (Mouse/Keyboard DPI & Macros)
        "logioptionsplus_agent",
        "logioptionsplus_appbroker",

        // Audio & Bluetooth (Headsets & Controllers)
        "RtkAudioService64",
        "RtkBtManServ",
        "audiodg",

        // GPU Drivers (NVIDIA & Intel Hybrid Display)
        "nvcontainer",
        "NVDisplay.Container",
        "NVIDIA Overlay",
        "NVIDIA Share",
        "igfxEM",
        "igfxCUIService",
        "IntelCpHDCPSvc",
        "IntelCpHeciSvc",

        // Synaptics Touchpad (Palm Rejection while typing WASD)
        "SynTPEnh",
        "SynTPEnhService",

        // Common Optional Gaming Voice/Chat (if user opens Discord)
        "Discord"
    ];

    // Explicit Blacklist / Kill List (Killed even if located inside Windows/DriverStore/SystemApps)
    public List<string> ExplicitBlockedProcesses { get; set; } =
    [
        // HP Bloatware & Telemetry
        "TouchpointAnalyticsClientService",
        "SysInfoCap",
        "DiagsCap",
        "NetworkCap",
        "AppHelperCap",
        "BridgeCommunication",
        "OmenCommandCenterBackground",
        "HP.Omen.OmenCommandCenter",
        "HPSystemEventUtilityHost",

        // Background Updaters & Office
        "OfficeClickToRun",
        "logioptionsplus_updater",
        "sqlwriter",
        "PresentationFontCache",
        "GoogleUpdate",
        "MicrosoftEdgeUpdate",
        "BraveUpdate",
        "AdobeARM",
        "Acrocef",

        // Windows 11 Bloat / Widgets / Search / Feeds
        "MicrosoftStartFeedProvider",
        "WidgetBoard",
        "WidgetService",
        "CrossDeviceResume",
        "SearchHost",
        "SearchIndexer",
        "UserOOBEBroker",
        "SystemSettings",
        "PhoneExperienceHost",
        "YourPhone",
        "GameBar",
        "GameBarFTServer",

        // Non-essential Tray, IDEs & Utilities on this laptop
        "Antigravity",
        "language_server",
        "Sleekshot",
        "OneApp.IGCC.WinService",
        "RtkNGUI64",
        "SecurityHealthSystray",
        "OneDrive"
    ];

    // Windows Services to pause when entering Game Mode and restore when exiting
    public List<string> ServicesToPause { get; set; } =
    [
        // HP Bloatware & Analytics Services
        "HpTouchpointAnalyticsService",
        "HPSysInfoCap",
        "HPDiagsCap",
        "HPAppHelperCap",
        "HPNetworkCap",

        // Windows Search, Superfetch & Updates
        "WSearch",
        "SysMain",
        "UsoSvc",
        "wuauserv",
        "DoSvc",

        // Background Updaters & Non-Gaming Services
        "ClickToRunSvc",
        "OptionsPlusUpdaterService",
        "SQLWriter",
        "igccservice",
        "FontCache3.0.0.0",
        "PcaSvc",
        "DPS",
        "DiagTrack",
        "lfsvc",
        "MapsBroker",
        "Spooler"
    ];

    // Absolute Kernel & Windows Session Safe-List (NEVER touch under any circumstance)
    [JsonIgnore]
    public static readonly HashSet<string> KernelSafeList = new(StringComparer.OrdinalIgnoreCase)
    {
        "System",
        "Idle",
        "Registry",
        "Memory Compression",
        "Secure System",
        "smss",
        "csrss",
        "wininit",
        "winlogon",
        "services",
        "lsass",
        "LsaIso",
        "svchost",
        "dwm",
        "explorer",
        "sihost",
        "ShellHost",
        "StartMenuExperienceHost",
        "ShellExperienceHost",
        "LockApp",
        "LogonUI",
        "ApplicationFrameHost",
        "RuntimeBroker",
        "taskhostw",
        "dllhost",
        "conhost",
        "fontdrvhost",
        "WUDFHost",
        "dasHost",
        "wlanext",
        "WmiPrvSE",
        "unsecapp",
        "ctfmon",
        "TextInputHost",
        "chsime",
        "MsMpEng",
        "MpDefenderCoreService",
        "NisSrv",
        "SecurityHealthService",
        "DefenderAiPlatformHost",
        "DefenderSessionHelper",
        "SecurityHealthHost",
        "SmartScreen",
        "spoolsv",
        "audiodg",
        "SearchProtocolHost",
        "SearchFilterHost",
        "cmd",
        "powershell",
        "pwsh",
        "OpenConsole",
        "WindowsTerminal",
        "GameGuard",
        "GameGuard_Setup"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string ConfigFilePath
    {
        get
        {
            // If running from Program Files (installed for all users), store config in C:\ProgramData\GameGuard
            string baseDir = AppContext.BaseDirectory;
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (baseDir.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                baseDir.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase))
            {
                string commonDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "GameGuard");
                try
                {
                    Directory.CreateDirectory(commonDir);
                    return Path.Combine(commonDir, "config.json");
                }
                catch
                {
                    string userDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "GameGuard");
                    Directory.CreateDirectory(userDir);
                    return Path.Combine(userDir, "config.json");
                }
            }

            return Path.Combine(baseDir, "config.json");
        }
    }

    public static GameGuardConfig Load()
    {
        GameGuardConfig? config = null;
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                config = JsonSerializer.Deserialize<GameGuardConfig>(json, JsonOptions);
            }
        }
        catch
        {
            // Fallback to default config if corrupted
        }

        var defaults = new GameGuardConfig();
        config ??= defaults;

        // Ensure any newly added default Whitelist entries (e.g. PUBG, Anti-Cheat, gameoverlayui64) are present
        foreach (string defaultWhite in defaults.WhitelistedProcesses)
        {
            if (!config.WhitelistedProcesses.Contains(defaultWhite, StringComparer.OrdinalIgnoreCase) &&
                !config.ExplicitBlockedProcesses.Contains(defaultWhite, StringComparer.OrdinalIgnoreCase))
            {
                config.WhitelistedProcesses.Add(defaultWhite);
            }
        }

        // Ensure newly added default ExplicitBlockedProcesses (e.g. Antigravity, language_server) are present unless whitelisted
        foreach (string defaultBlack in defaults.ExplicitBlockedProcesses)
        {
            if (!config.ExplicitBlockedProcesses.Contains(defaultBlack, StringComparer.OrdinalIgnoreCase) &&
                !config.WhitelistedProcesses.Contains(defaultBlack, StringComparer.OrdinalIgnoreCase))
            {
                config.ExplicitBlockedProcesses.Add(defaultBlack);
            }
        }

        // Automatically detect current machine hardware and populate dynamic rules if machine changed or first run
        try
        {
            var profile = HardwareProfileDetector.DetectCurrentMachine();
            string signature = $"{profile.Manufacturer}|{profile.Model}|{profile.CpuVendor}|{string.Join(",", profile.Gpus)}";
            if (!string.Equals(config.LastDetectedMachineSignature, signature, StringComparison.OrdinalIgnoreCase))
            {
                config.LastDetectedMachineSignature = signature;
                HardwareProfileDetector.ApplyDynamicHardwareRules(config, profile);
            }
            else
            {
                config.Save();
            }
        }
        catch
        {
            config.Save();
        }

        return config;
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // Ignore write errors
        }
    }
}
