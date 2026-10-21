using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using GameGuard.Models;

namespace GameGuard.Services;

public sealed class MachineHardwareProfile
{
    public string Manufacturer { get; set; } = "Genérico";
    public string Model { get; set; } = "PC";
    public string BrandCategory { get; set; } = "Custom"; // HP, Lenovo, ASUS, MSI, Acer, Dell, Sony, Samsung, Gigabyte, Razer, Custom
    public bool IsLaptop { get; set; }
    public string CpuVendor { get; set; } = "Desconocido"; // Intel, AMD
    public string CpuName { get; set; } = "";
    public List<string> Gpus { get; set; } = [];
    public bool HasNvidiaGpu => Gpus.Any(g => g.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
    public bool HasAmdGpu => Gpus.Any(g => g.Contains("AMD", StringComparison.OrdinalIgnoreCase) || g.Contains("Radeon", StringComparison.OrdinalIgnoreCase));
    public bool HasIntelGpu => Gpus.Any(g => g.Contains("Intel", StringComparison.OrdinalIgnoreCase));

    public string SummaryText =>
        $"{BrandCategory} ({(IsLaptop ? "Laptop" : "Escritorio")}) • CPU {CpuVendor} • GPU {string.Join(" + ", Gpus.Select(ShortGpuVendor).Distinct())}";

    private static string ShortGpuVendor(string gpuName)
    {
        if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
        if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD Radeon";
        if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
        return gpuName;
    }
}

public static class HardwareProfileDetector
{
    public static MachineHardwareProfile DetectCurrentMachine()
    {
        var profile = new MachineHardwareProfile();

        // 1. Manufacturer, Model & PCSystemType (1 = Desktop, 2 = Mobile/Laptop)
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, PCSystemType FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                profile.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "Genérico";
                profile.Model = obj["Model"]?.ToString()?.Trim() ?? "PC";
                ushort sysType = Convert.ToUInt16(obj["PCSystemType"] ?? 1);
                profile.IsLaptop = sysType == 2;
            }
        }
        catch { }

        // Also check battery presence just in case PCSystemType wasn't 2
        if (!profile.IsLaptop)
        {
            try
            {
                using var batSearcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
                if (batSearcher.Get().Count > 0)
                    profile.IsLaptop = true;
            }
            catch { }
        }

        profile.BrandCategory = ClassifyBrand(profile.Manufacturer, profile.Model);

        // 2. CPU Vendor & Name
        try
        {
            using var cpuSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Name FROM Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
            {
                string mfg = obj["Manufacturer"]?.ToString() ?? "";
                string name = obj["Name"]?.ToString()?.Trim() ?? "";
                profile.CpuName = name;

                if (mfg.Contains("Intel", StringComparison.OrdinalIgnoreCase) || name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    profile.CpuVendor = "Intel";
                else if (mfg.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Ryzen", StringComparison.OrdinalIgnoreCase))
                    profile.CpuVendor = "AMD";
            }
        }
        catch { }

        // 3. GPUs
        try
        {
            using var gpuSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (ManagementObject obj in gpuSearcher.Get())
            {
                string? gpuName = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(gpuName))
                {
                    profile.Gpus.Add(gpuName);
                }
            }
        }
        catch { }

        return profile;
    }

    private static string ClassifyBrand(string manufacturer, string model)
    {
        string combined = $"{manufacturer} {model}".ToUpperInvariant();

        if (combined.Contains("HP") || combined.Contains("HEWLETT") || combined.Contains("OMEN") || combined.Contains("VICTUS"))
            return "HP";
        if (combined.Contains("LENOVO") || combined.Contains("LEGION") || combined.Contains("IDEA") || combined.Contains("THINKPAD") || combined.Contains("LOQ"))
            return "Lenovo";
        if (combined.Contains("ASUS") || combined.Contains("ROG") || combined.Contains("TUF"))
            return "ASUS";
        if (combined.Contains("MSI") || combined.Contains("MICRO-STAR"))
            return "MSI";
        if (combined.Contains("ACER") || combined.Contains("PREDATOR") || combined.Contains("NITRO"))
            return "Acer";
        if (combined.Contains("DELL") || combined.Contains("ALIENWARE"))
            return "Dell/Alienware";
        if (combined.Contains("SONY") || combined.Contains("VAIO"))
            return "Sony VAIO";
        if (combined.Contains("GIGABYTE") || combined.Contains("AORUS"))
            return "Gigabyte";
        if (combined.Contains("RAZER"))
            return "Razer";
        if (combined.Contains("SAMSUNG"))
            return "Samsung";

        return "PC Genérica / Custom";
    }

    /// <summary>
    /// Dynamically builds or updates the Whitelist, Blacklist, and ServicesToPause based on the detected
    /// hardware (OEM brand, Laptop vs Desktop, Intel vs AMD, NVIDIA vs Radeon) AND heuristics on running processes.
    /// </summary>
    public static (int AddedWhite, int AddedBlack, int AddedServices) ApplyDynamicHardwareRules(
        GameGuardConfig config,
        MachineHardwareProfile profile)
    {
        var white = new HashSet<string>(config.WhitelistedProcesses, StringComparer.OrdinalIgnoreCase);
        var black = new HashSet<string>(config.ExplicitBlockedProcesses, StringComparer.OrdinalIgnoreCase);
        var svcs = new HashSet<string>(config.ServicesToPause, StringComparer.OrdinalIgnoreCase);

        int initWhite = white.Count;
        int initBlack = black.Count;
        int initSvcs = svcs.Count;

        // =========================================================================
        // 1. UNIVERSAL WINDOWS & COMMON UPDATERS (Applies to ALL PCs)
        // =========================================================================
        AddRange(black, [
            "OfficeClickToRun", "MicrosoftStartFeedProvider", "WidgetBoard", "WidgetService",
            "CrossDeviceResume", "SearchHost", "SearchIndexer", "UserOOBEBroker", "SystemSettings",
            "PhoneExperienceHost", "YourPhone", "GameBar", "GameBarFTServer", "OneDrive",
            "GoogleUpdate", "MicrosoftEdgeUpdate", "BraveUpdate", "OperaAutoupdate",
            "AdobeARM", "Acrocef", "CCXProcess", "CreativeCloud", "AdobeIPCBroker",
            "sqlwriter", "PresentationFontCache", "SecurityHealthSystray", "Cortana", "Teams",
            "Antigravity", "language_server"
        ]);

        AddRange(svcs, [
            "WSearch", "SysMain", "UsoSvc", "wuauserv", "DoSvc", "ClickToRunSvc",
            "SQLWriter", "FontCache3.0.0.0", "PcaSvc", "DPS", "DiagTrack",
            "lfsvc", "MapsBroker", "Spooler", "RetailDemo", "Fax"
        ]);

        // =========================================================================
        // 2. PERIPHERAL GAMING BRANDS & ANTI-CHEAT / PUBG / STEAM OVERLAYS
        //    Keep Mouse/Headset Agents & Anti-Cheats in Whitelist; Updaters in Blacklist
        // =========================================================================
        AddRange(white, [
            "steam", "steamservice", "steamwebhelper", "GameOverlayUI", "gameoverlayui64",
            "TslGame", "TslGame_BE", "TslGame_ZK", "ExecPubg", "UnrealCEFSubProcess_3071",
            "UnrealCEFSubProcess", "iigw_server", "BroCrashReporter",
            "BEService", "BEService_pubg", "BEService_x64", "zksvc",
            "EasyAntiCheat", "EasyAntiCheat_EOS",
            "logioptionsplus_agent", "logioptionsplus_appbroker", "lghub_agent", "LCore",
            "Razer Synapse Service", "RazerCentralService",
            "iCUE", "Corsair.Service",
            "SteelSeriesGGClient", "SteelSeriesSonar",
            "NGenuity2Helper"
        ]);

        AddRange(black, [
            "logioptionsplus_updater", "lghub_updater", "LogiOverlay",
            "RazerIngameEngine", "GameManagerService3", "RazerAxon", "Razer Cortex",
            "Corsair.Service.CpuIdRemote", "SteelSeriesUpdateService"
        ]);

        AddRange(svcs, [
            "OptionsPlusUpdaterService", "LGHUBUpdaterService", "GameManagerService3"
        ]);

        // =========================================================================
        // 3. LAPTOP vs DESKTOP SPECIFIC RULES
        // =========================================================================
        if (profile.IsLaptop)
        {
            // Protect Touchpad drivers (Synaptics, ELAN, ASUS Precision) for palm rejection
            AddRange(white, [
                "SynTPEnh", "SynTPEnhService", "ETDCtrl", "ETDCtrlHelper", "ETDService", "AsusTPHelper"
            ]);
        }
        else
        {
            // On a Desktop PC, touchpad services and mobile sync are 100% unnecessary
            AddRange(black, ["SynTPEnh", "ETDCtrl"]);
        }

        // =========================================================================
        // 4. CPU VENDOR RULES (INTEL vs AMD RYZEN)
        // =========================================================================
        if (profile.CpuVendor.Equals("Intel", StringComparison.OrdinalIgnoreCase))
        {
            // Keep Intel thermal & tuning daemon
            AddRange(white, ["esif_uf", "XtuService"]);
            // Block Intel Driver Updater & Telemetry
            AddRange(black, ["DSAService", "DSAUpdateService", "DSATray", "IntelAnalyticsService", "IntelTelemetryService"]);
            AddRange(svcs, ["DSAService", "DSAUpdateService", "IntelAnalyticsService"]);
        }
        else if (profile.CpuVendor.Equals("AMD", StringComparison.OrdinalIgnoreCase))
        {
            // Keep AMD Ryzen Master / Chipset power helpers
            AddRange(white, ["AMDRyzenMasterDriver", "amdryzenmaster"]);
            // Block AMD User Experience Telemetry
            AddRange(black, ["AUEPMaster", "AUEPLauncher"]);
            AddRange(svcs, ["AUEPLauncher"]);
        }

        // =========================================================================
        // 5. GPU VENDOR RULES (NVIDIA vs AMD RADEON vs INTEL ARC/IRIS)
        // =========================================================================
        if (profile.HasNvidiaGpu)
        {
            AddRange(white, ["nvcontainer", "NVDisplay.Container", "NVIDIA Overlay", "NVIDIA Share", "nvsphelper64"]);
            AddRange(black, ["NvTelemetryContainer", "NVIDIA Web Helper"]);
            AddRange(svcs, ["NvTelemetryContainer"]);
        }

        if (profile.HasAmdGpu)
        {
            AddRange(white, ["atiesrxx", "atieclxx", "RadeonSoftware", "AMDRSServ"]);
            AddRange(black, ["AUEPMaster", "AMDBugReportTool"]);
        }

        if (profile.HasIntelGpu)
        {
            AddRange(white, ["igfxEM", "igfxCUIService", "IntelCpHDCPSvc", "IntelCpHeciSvc"]);
            // If machine ALSO has a dedicated NVIDIA or AMD GPU, Intel Graphics Command Center UI/Service can be paused while gaming
            if (profile.HasNvidiaGpu || profile.HasAmdGpu)
            {
                AddRange(black, ["OneApp.IGCC.WinService", "IGCCTray", "IGCC"]);
                AddRange(svcs, ["igccservice"]);
            }
        }

        // =========================================================================
        // 6. OEM BRAND-SPECIFIC RULES (HP, Lenovo, ASUS, MSI, Acer, Dell, Sony, etc.)
        // =========================================================================
        switch (profile.BrandCategory)
        {
            case "HP":
                AddRange(white, ["OmenCap", "XtuService"]);
                AddRange(black, [
                    "TouchpointAnalyticsClientService", "SysInfoCap", "DiagsCap", "NetworkCap",
                    "AppHelperCap", "BridgeCommunication", "OmenCommandCenterBackground",
                    "HP.Omen.OmenCommandCenter", "HPSystemEventUtilityHost", "HPJumpStartBridge",
                    "HPJumpStartLaunch", "HPSupportSolutionsFrameworkService", "HPCommRecovery"
                ]);
                AddRange(svcs, [
                    "HpTouchpointAnalyticsService", "HPSysInfoCap", "HPDiagsCap", "HPAppHelperCap",
                    "HPNetworkCap", "HPPrintScanDoctorService", "HPSupportSolutionsFrameworkService"
                ]);
                break;

            case "Lenovo":
                // Keep Lenovo Intelligent Thermal Solution (Fn+Q thermal modes)
                AddRange(white, ["LITSSvc", "LenovoHotkeys"]);
                AddRange(black, [
                    "LenovoVantage", "LenovoVantageService", "Lenovo.Modern.ImController",
                    "Lenovo.Modern.ImController.PluginHost", "UDClientService", "LenovoArena", "LenovoNow"
                ]);
                AddRange(svcs, ["ImControllerService", "LenovoVantageService", "UDClientService"]);
                break;

            case "ASUS":
                // Keep AsusOptimization (Fn+F5 fan profile control)
                AddRange(white, ["AsusOptimization", "ATKOSD2"]);
                AddRange(black, [
                    "ArmouryCrate", "ArmouryCrate.UserSessionHelper", "ArmourySocketServer",
                    "AsusFramework", "AsusSoftwareManager", "AsusSoftwareManagerAgent",
                    "AsusSwitch", "AsusLinkNear", "AsusLinkRemote", "MyASUS", "GameSDK"
                ]);
                AddRange(svcs, [
                    "ArmouryCrateService", "AsusAppService", "ASUSLinkNear",
                    "ASUSLinkRemote", "AsusSoftwareManager"
                ]);
                break;

            case "MSI":
                AddRange(white, ["MSICentralService"]);
                AddRange(black, [
                    "MSI.Companion", "MSI.TerminalServer", "MSI Center", "Dragon Center",
                    "MSI.VoiceControlService", "NahimicCompanion", "VoiceControlEngine"
                ]);
                AddRange(svcs, ["MSI_Companion_Service", "MSI_VoiceControl_Service", "LightKeeperService"]);
                break;

            case "Acer":
                // Keep PredatorSense / NitroSense fan service, close heavy UI & telemetry
                AddRange(white, ["PSAdminService", "PSSvc", "NitroSenseService"]);
                AddRange(black, [
                    "PredatorSense", "NitroSense", "AcerCareCenter", "ACCStd",
                    "AcerAgentSvc", "AcerJumpStart", "AcerProductRegistration", "UEIPSvc"
                ]);
                AddRange(svcs, ["ACCtSvc", "UEIPSvc", "AcerAgentSvc"]);
                break;

            case "Dell/Alienware":
                AddRange(white, ["AWCCService", "AWCC.Background.Server"]);
                AddRange(black, [
                    "SupportAssistAgent", "DellDataVault", "DellDataVaultCollector",
                    "DellTechHub", "Dell.TechHub", "SubAgent", "DDVDataCollector", "AWCC"
                ]);
                AddRange(svcs, [
                    "SupportAssistAgent", "DDVDataCollector", "DDVRulesProcessor",
                    "DDVServiceMapper", "DellTechHub", "DellClientManagementService"
                ]);
                break;

            case "Sony VAIO":
                AddRange(white, ["VESMgr", "VESMgrSub", "SPMgr"]);
                AddRange(black, [
                    "VAIOCare", "VCSystemTray", "VAIOMessenger", "VAIOUpdate",
                    "VAIOSampleCollector", "VAIOGate"
                ]);
                AddRange(svcs, ["SampleCollector", "VAIOCareService", "VAIOUpdateService"]);
                break;

            case "Samsung":
                AddRange(white, ["SamsungSystemSupportEngine"]);
                AddRange(black, ["SamsungMagician", "SamsungUpdate", "QuickShare"]);
                AddRange(svcs, ["SamsungMagicianSvc", "SamsungUpdateService"]);
                break;

            case "Gigabyte":
                AddRange(black, ["GCC", "GigabyteUpdateService", "RGBFusion", "AppCenter"]);
                AddRange(svcs, ["GigabyteUpdateService"]);
                break;
        }

        // =========================================================================
        // 7. HEURISTIC LIVE SCAN OF CURRENT MACHINE'S PROCESSES & SERVICES
        //    Automatically detects unknown 3rd-party Updaters, Telemetry & Analytics
        // =========================================================================
        ScanRunningProcessesHeuristically(white, black);
        ScanRunningServicesHeuristically(svcs);

        // Ensure nothing in Whitelist is also in Blacklist
        foreach (string w in white)
        {
            black.Remove(w);
        }

        config.WhitelistedProcesses = white.OrderBy(x => x).ToList();
        config.ExplicitBlockedProcesses = black.OrderBy(x => x).ToList();
        config.ServicesToPause = svcs.OrderBy(x => x).ToList();
        config.Save();

        return (
            Math.Max(0, white.Count - initWhite),
            Math.Max(0, black.Count - initBlack),
            Math.Max(0, svcs.Count - initSvcs)
        );
    }

    private static void ScanRunningProcessesHeuristically(HashSet<string> white, HashSet<string> black)
    {
        string[] bloatKeywords =
        [
            "Telemetry", "Analytics", "Updater", "UpdateService", "DiagCap", "DiagsCap",
            "CrashReport", "ErrorReporter", "Feedback", "JumpStart", "SupportAssist"
        ];

        foreach (var proc in Process.GetProcesses())
        {
            using (proc)
            {
                try
                {
                    string name = ProcessGuardian.NormalizeName(proc.ProcessName);
                    if (string.IsNullOrEmpty(name) ||
                        GameGuardConfig.KernelSafeList.Contains(name) ||
                        white.Contains(name))
                    {
                        continue;
                    }

                    if (bloatKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        black.Add(name);
                    }
                }
                catch { }
            }
        }
    }

    private static void ScanRunningServicesHeuristically(HashSet<string> svcs)
    {
        string[] safeProtectedServicePrefixes =
        [
            "Rpc", "Dcom", "WinDefend", "MDCore", "WdNis", "mpssvc", "BFE", "Dhcp", "Dnscache",
            "Wlan", "Audio", "PlugPlay", "Power", "EventLog", "ProfSvc", "LSM", "SamSs", "VaultSvc",
            "KeyIso", "CryptSvc", "nsi", "NlaSvc", "netprofm", "Wcmsvc", "Schedule", "UserManager"
        ];

        string[] telemetryOrUpdaterKeywords =
        [
            "Telemetry", "Analytics", "Updater", "Diagnostics HSA", "Diagnostic", "Update Service", "SupportAssist"
        ];

        try
        {
            foreach (var sc in ServiceController.GetServices())
            {
                using (sc)
                {
                    string svcName = sc.ServiceName;
                    string dispName = sc.DisplayName ?? "";

                    if (safeProtectedServicePrefixes.Any(p => svcName.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (telemetryOrUpdaterKeywords.Any(k =>
                            svcName.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                            dispName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        svcs.Add(svcName);
                    }
                }
            }
        }
        catch { }
    }

    private static void AddRange(HashSet<string> set, IEnumerable<string> items)
    {
        foreach (string item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
                set.Add(item);
        }
    }
}
