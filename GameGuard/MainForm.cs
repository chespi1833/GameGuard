using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Principal;
using System.ServiceProcess;
using GameGuard.Models;
using GameGuard.Services;

namespace GameGuard;

public sealed class MainForm : Form
{
    private readonly GameGuardConfig _config;
    private readonly MachineHardwareProfile _hardwareProfile;
    private readonly ProcessGuardian _guardian;
    private readonly WindowsServiceManager _serviceManager;
    private readonly PowerPlanManager _powerPlanManager;

    private bool _isGameModeActive;
    private bool _isBusy;
    private double _sessionRamFreedMB;
    private bool _allowClose;

    // UI Controls
    private Label _lblStatusBadge = null!;
    private Label _lblAdminBadge = null!;
    private Button _btnElevate = null!;
    private Button _btnToggleGameMode = null!;
    private Button _btnPurgeRamNow = null!;
    private Button _btnSimulate = null!;

    // Telemetry Cards
    private Label _lblCardRamValue = null!;
    private Label _lblCardRamSub = null!;
    private Label _lblCardProcsValue = null!;
    private Label _lblCardProcsSub = null!;
    private Label _lblCardServicesValue = null!;
    private Label _lblCardServicesSub = null!;
    private Label _lblCardGameValue = null!;
    private Label _lblCardGameSub = null!;

    // Feature Checkboxes
    private CheckBox _chkSteamChildren = null!;
    private CheckBox _chkDeepRam = null!;
    private CheckBox _chkPowerPlan = null!;
    private CheckBox _chkSteamLite = null!;
    private CheckBox _chkStrictWhitelist = null!;

    // Log & Lists
    private RichTextBox _txtLog = null!;
    private ListBox _lstWhitelist = null!;
    private ListBox _lstBlacklist = null!;
    private ComboBox _cmbRunningProcs = null!;
    private TextBox _txtNewProcess = null!;

    // System Tray & Timers
    private NotifyIcon _trayIcon = null!;
    private ToolStripMenuItem _trayToggleItem = null!;
    private System.Windows.Forms.Timer _uiTimer = null!;

    // Theme Colors (Dark Gaming Palette)
    private static readonly Color BgDark = Color.FromArgb(15, 17, 23);
    private static readonly Color BgCard = Color.FromArgb(25, 29, 38);
    private static readonly Color BgInput = Color.FromArgb(34, 39, 51);
    private static readonly Color OmenRed = Color.FromArgb(230, 0, 46);
    private static readonly Color NeonGreen = Color.FromArgb(0, 214, 114);
    private static readonly Color AccentBlue = Color.FromArgb(56, 189, 248);
    private static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public MainForm()
    {
        _hardwareProfile = HardwareProfileDetector.DetectCurrentMachine();
        _config = GameGuardConfig.Load();
        _guardian = new ProcessGuardian(_config);
        _serviceManager = new WindowsServiceManager();
        _powerPlanManager = new PowerPlanManager();

        _guardian.OnLog += AppendLogThreadSafe;
        _guardian.OnProcessTerminated += OnGuardianProcessTerminated;
        _guardian.OnGameDetected += OnGuardianGameDetected;

        InitializeComponent();
        SetupTrayIcon();
        LoadConfigIntoUi();
        _guardian.RefreshLauncherAndGameTree();
        UpdateTelemetryUi();

        _uiTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _uiTimer.Tick += (_, _) => UpdateTelemetryUi();
        _uiTimer.Start();

        AppendLog($"💻 Hardware detectado: {_hardwareProfile.Manufacturer} {_hardwareProfile.Model} [{_hardwareProfile.SummaryText}]");
        AppendLog("⚡ Reglas dinámicas Multimarca (OEM, CPU, GPU y Heurística) cargadas en config.json.");
        if (!IsRunningAsAdmin())
        {
            AppendLog("⚠️ Ejecutando sin permisos de Administrador. Haz clic en 'Elevar como Admin' arriba a la derecha para poder detener servicios de Windows y activar el filtro WMI de kernel.");
        }
    }

    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void InitializeComponent()
    {
        Text = "GameGuard — Guardián de Modo Juego y Lista Blanca";
        Size = new Size(980, 700);
        MinimumSize = new Size(920, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgDark;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        Icon = CreateDynamicIcon(OmenRed);

        // Top Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.FromArgb(20, 23, 31),
            Padding = new Padding(20, 12, 20, 12)
        };

        var lblTitle = new Label
        {
            Text = "GAMEGUARD",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = OmenRed,
            AutoSize = true,
            Location = new Point(20, 10)
        };

        var lblSubtitle = new Label
        {
            Text = $"💻 Equipo: {_hardwareProfile.SummaryText} • Guardián en Tiempo Real Multimarca",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(22, 39)
        };

        _lblStatusBadge = new Label
        {
            Text = "● MODO NORMAL",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(780, 14)
        };

        bool isAdmin = IsRunningAsAdmin();
        _lblAdminBadge = new Label
        {
            Text = isAdmin ? "🛡️ Permisos: Administrador" : "⚠️ Permisos: Usuario Estándar",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = isAdmin ? NeonGreen : Color.Orange,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(780, 38)
        };

        _btnElevate = new Button
        {
            Text = "⚡ Elevar como Admin",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(217, 119, 6),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(150, 32),
            Location = new Point(615, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            Visible = !isAdmin
        };
        _btnElevate.FlatAppearance.BorderSize = 0;
        _btnElevate.Click += (_, _) => RelaunchAsAdmin();

        headerPanel.Controls.AddRange([lblTitle, lblSubtitle, _lblStatusBadge, _lblAdminBadge, _btnElevate]);

        // Main Action Bar
        var actionPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(20, 12, 20, 12),
            BackColor = BgDark
        };

        _btnToggleGameMode = new Button
        {
            Text = "🚀 ACTIVAR MODO JUEGO (PURGAR Y VIGILAR)",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            BackColor = OmenRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(460, 50),
            Location = new Point(20, 13),
            Cursor = Cursors.Hand
        };
        _btnToggleGameMode.FlatAppearance.BorderSize = 0;
        _btnToggleGameMode.Click += async (_, _) => await ToggleGameModeAsync();

        _btnPurgeRamNow = new Button
        {
            Text = "🧹 Purgar RAM Ahora",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = BgCard,
            ForeColor = AccentBlue,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(210, 50),
            Location = new Point(495, 13),
            Cursor = Cursors.Hand
        };
        _btnPurgeRamNow.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
        _btnPurgeRamNow.Click += (_, _) => ExecuteManualRamPurge();

        _btnSimulate = new Button
        {
            Text = "🔍 Simular (Ver qué cerraría)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = BgCard,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 50),
            Location = new Point(718, 13),
            Cursor = Cursors.Hand
        };
        _btnSimulate.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
        _btnSimulate.Click += (_, _) => RunDryRunSimulation();

        actionPanel.Controls.AddRange([_btnToggleGameMode, _btnPurgeRamNow, _btnSimulate]);

        // 4 Telemetry Cards Row
        var cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(16, 4, 16, 8),
            BackColor = BgDark
        };
        for (int i = 0; i < 4; i++)
            cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        cardsPanel.Controls.Add(CreateTelemetryCard("MEMORIA RAM LIBRE", out _lblCardRamValue, out _lblCardRamSub, AccentBlue), 0, 0);
        cardsPanel.Controls.Add(CreateTelemetryCard("PROCESOS NEUTRALIZADOS", out _lblCardProcsValue, out _lblCardProcsSub, OmenRed), 1, 0);
        cardsPanel.Controls.Add(CreateTelemetryCard("SERVICIOS EN PAUSA", out _lblCardServicesValue, out _lblCardServicesSub, Color.FromArgb(250, 204, 21)), 2, 0);
        cardsPanel.Controls.Add(CreateTelemetryCard("JUEGO Y PLAN DE ENERGÍA", out _lblCardGameValue, out _lblCardGameSub, NeonGreen), 3, 0);

        // TabControl for Dashboard vs Whitelist Editor
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Padding = new Point(14, 6)
        };

        var tabDashboard = new TabPage("⚡ Panel de Control y Registro en Vivo")
        {
            BackColor = BgDark,
            ForeColor = TextPrimary,
            Padding = new Padding(14)
        };

        var tabLists = new TabPage("📋 Configurar Lista Blanca y Lista Negra")
        {
            BackColor = BgDark,
            ForeColor = TextPrimary,
            Padding = new Padding(14)
        };

        BuildDashboardTab(tabDashboard);
        BuildListsTab(tabLists);

        tabControl.TabPages.Add(tabDashboard);
        tabControl.TabPages.Add(tabLists);

        Controls.Add(tabControl);
        Controls.Add(cardsPanel);
        Controls.Add(actionPanel);
        Controls.Add(headerPanel);
    }

    private Panel CreateTelemetryCard(string title, out Label valueLabel, out Label subLabel, Color accentColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            BackColor = BgCard,
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 7.8f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(12, 8)
        };

        valueLabel = new Label
        {
            Text = "--",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = accentColor,
            AutoSize = true,
            Location = new Point(10, 26)
        };

        subLabel = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 8.2f, FontStyle.Regular),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(12, 54)
        };

        card.Controls.AddRange([lblTitle, valueLabel, subLabel]);
        return card;
    }

    private void BuildDashboardTab(TabPage page)
    {
        var dashboardGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BgDark
        };
        // 38% for Options, 62% (~60%) for the Live Log Screen
        dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        dashboardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        dashboardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Left: Feature Checkboxes (38%)
        var optionsBox = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgCard,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(14)
        };

        var lblOptsHeader = new Label
        {
            Text = "CARACTERÍSTICAS ACTIVAS DEL MODO JUEGO",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AccentBlue,
            AutoSize = true,
            Location = new Point(14, 12)
        };

        _chkSteamChildren = CreateFeatureCheckbox(
            "Autorizar hijos de Steam + Prioridad CPU Alta",
            new Point(14, 44));
        _chkDeepRam = CreateFeatureCheckbox(
            "Purgado profundo de RAM (Working Set + Standby)",
            new Point(14, 86));
        _chkPowerPlan = CreateFeatureCheckbox(
            "Activar plan de energía 'Ultimate Performance'",
            new Point(14, 128));
        _chkSteamLite = CreateFeatureCheckbox(
            "Modo 'Steam Ligero' (Reducir RAM steamwebhelper)",
            new Point(14, 170));
        _chkStrictWhitelist = CreateFeatureCheckbox(
            "Lista Blanca Estricta (Bloquear apps no listadas)",
            new Point(14, 212));

        var lblNote = new Label
        {
            Text = "💡 Nota: Los demonios térmicos del equipo, controladores de periféricos y sistemas Anti-Cheat se mantienen protegidos automáticamente en la Lista Blanca.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = TextMuted,
            Size = new Size(310, 75),
            Location = new Point(14, 260)
        };

        optionsBox.Controls.AddRange([
            lblOptsHeader,
            _chkSteamChildren,
            _chkDeepRam,
            _chkPowerPlan,
            _chkSteamLite,
            _chkStrictWhitelist,
            lblNote
        ]);

        // Right: Live Activity Log (62% of bottom space)
        var logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgCard,
            Margin = new Padding(6, 0, 0, 0),
            Padding = new Padding(12)
        };

        var lblLogHeader = new Label
        {
            Text = "REGISTRO DE ACTIVIDAD Y PROCESOS INTERCEPTADOS EN TIEMPO REAL",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextMuted,
            Dock = DockStyle.Top,
            Height = 24
        };

        _txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 14, 19),
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Consolas", 9.5f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };

        logPanel.Controls.Add(_txtLog);
        logPanel.Controls.Add(lblLogHeader);

        dashboardGrid.Controls.Add(optionsBox, 0, 0);
        dashboardGrid.Controls.Add(logPanel, 1, 0);
        page.Controls.Add(dashboardGrid);
    }

    private CheckBox CreateFeatureCheckbox(string text, Point location)
    {
        var chk = new CheckBox
        {
            Text = text,
            Location = location,
            Size = new Size(310, 32),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        chk.CheckedChanged += (_, _) => SaveUiToConfig();
        return chk;
    }

    private void BuildListsTab(TabPage page)
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = BgDark
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));

        // Left: Whitelist Panel
        var whitePanel = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Margin = new Padding(4), Padding = new Padding(12) };
        var lblWhite = new Label
        {
            Text = "✅ LISTA BLANCA (Procesos permitidos al jugar)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = NeonGreen,
            Dock = DockStyle.Top,
            Height = 26
        };
        _lstWhitelist = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = BgInput,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9.5f)
        };
        var btnRemoveWhite = new Button
        {
            Text = "🗑️ Quitar seleccionado de Lista Blanca",
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRemoveWhite.FlatAppearance.BorderSize = 0;
        btnRemoveWhite.Click += (_, _) =>
        {
            if (_lstWhitelist.SelectedItem is string item)
            {
                _config.WhitelistedProcesses.RemoveAll(x => x.Equals(item, StringComparison.OrdinalIgnoreCase));
                _config.Save();
                RefreshListBoxes();
                PopulateRunningProcessesCombo();
            }
        };
        whitePanel.Controls.Add(_lstWhitelist);
        whitePanel.Controls.Add(btnRemoveWhite);
        whitePanel.Controls.Add(lblWhite);

        // Right: Blacklist Panel
        var blackPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Margin = new Padding(4), Padding = new Padding(12) };
        var lblBlack = new Label
        {
            Text = "🚫 LISTA NEGRA EXPLÍCITA (Matar y prohibir siempre)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = OmenRed,
            Dock = DockStyle.Top,
            Height = 26
        };
        _lstBlacklist = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = BgInput,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9.5f)
        };
        var btnRemoveBlack = new Button
        {
            Text = "🗑️ Quitar seleccionado de Lista Negra",
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRemoveBlack.FlatAppearance.BorderSize = 0;
        btnRemoveBlack.Click += (_, _) =>
        {
            if (_lstBlacklist.SelectedItem is string item)
            {
                _config.ExplicitBlockedProcesses.RemoveAll(x => x.Equals(item, StringComparison.OrdinalIgnoreCase));
                _config.Save();
                RefreshListBoxes();
                PopulateRunningProcessesCombo();
            }
        };
        blackPanel.Controls.Add(_lstBlacklist);
        blackPanel.Controls.Add(btnRemoveBlack);
        blackPanel.Controls.Add(lblBlack);

        // Bottom: Quick Add Bar
        var bottomBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgCard,
            Margin = new Padding(4),
            Padding = new Padding(12)
        };
        mainLayout.SetColumnSpan(bottomBar, 2);

        var lblAdd = new Label
        {
            Text = "Procesos abiertos no clasificados o buscar ejecutable (.exe):",
            AutoSize = true,
            Location = new Point(12, 10),
            ForeColor = TextMuted
        };

        _cmbRunningProcs = new ComboBox
        {
            Location = new Point(12, 34),
            Size = new Size(200, 28),
            BackColor = BgInput,
            ForeColor = TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbRunningProcs.SelectedIndexChanged += (_, _) =>
        {
            if (_cmbRunningProcs.SelectedItem is string selected)
                _txtNewProcess.Text = selected;
        };

        var btnRefreshProcs = new Button
        {
            Text = "🔄",
            Location = new Point(216, 33),
            Size = new Size(34, 28),
            BackColor = BgInput,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRefreshProcs.Click += (_, _) => PopulateRunningProcessesCombo();

        _txtNewProcess = new TextBox
        {
            Location = new Point(256, 34),
            Size = new Size(155, 28),
            BackColor = BgInput,
            ForeColor = TextPrimary,
            PlaceholderText = "ej: discord"
        };

        var btnBrowseExe = new Button
        {
            Text = "📂 Examinar...",
            Location = new Point(417, 32),
            Size = new Size(108, 32),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBrowseExe.FlatAppearance.BorderSize = 0;
        btnBrowseExe.Click += (_, _) => BrowseForExecutable();

        var btnAddWhite = new Button
        {
            Text = "➕ A Lista Blanca",
            Location = new Point(531, 32),
            Size = new Size(132, 32),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAddWhite.FlatAppearance.BorderSize = 0;
        btnAddWhite.Click += (_, _) => AddProcessToList(isWhitelist: true);

        var btnAddBlack = new Button
        {
            Text = "🚫 A Lista Negra",
            Location = new Point(669, 32),
            Size = new Size(132, 32),
            BackColor = OmenRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAddBlack.FlatAppearance.BorderSize = 0;
        btnAddBlack.Click += (_, _) => AddProcessToList(isWhitelist: false);

        var btnAutoDetect = new Button
        {
            Text = "🤖 Auto-Detectar",
            Location = new Point(807, 32),
            Size = new Size(125, 32),
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAutoDetect.FlatAppearance.BorderSize = 0;
        btnAutoDetect.Click += (_, _) =>
        {
            var prof = HardwareProfileDetector.DetectCurrentMachine();
            var (w, b, s) = HardwareProfileDetector.ApplyDynamicHardwareRules(_config, prof);
            RefreshListBoxes();
            PopulateRunningProcessesCombo();
            AppendLog($"🤖 Auto-Detección ejecutada ({prof.SummaryText}): +{w} en Lista Blanca, +{b} en Lista Negra, +{s} Servicios.");
            MessageBox.Show(
                $"Hardware detectado:\n• Fabricante/Modelo: {prof.Manufacturer} {prof.Model}\n• Tipo: {prof.SummaryText}\n\n" +
                $"Reglas aplicadas dinámicamente:\n• Nuevos en Lista Blanca: +{w}\n• Nuevos en Lista Negra: +{b}\n• Nuevos Servicios a pausar: +{s}",
                "Auto-Detección de Hardware Multimarca",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };

        bottomBar.Controls.AddRange([lblAdd, _cmbRunningProcs, btnRefreshProcs, _txtNewProcess, btnBrowseExe, btnAddWhite, btnAddBlack, btnAutoDetect]);

        mainLayout.Controls.Add(whitePanel, 0, 0);
        mainLayout.Controls.Add(blackPanel, 1, 0);
        mainLayout.Controls.Add(bottomBar, 0, 1);

        page.Controls.Add(mainLayout);
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();
        _trayToggleItem = new ToolStripMenuItem("🚀 Activar Modo Juego", null, async (_, _) => await ToggleGameModeAsync());
        var purgeItem = new ToolStripMenuItem("🧹 Purgar RAM Ahora", null, (_, _) => ExecuteManualRamPurge());
        var openItem = new ToolStripMenuItem("🖥️ Mostrar Panel GameGuard", null, (_, _) => ShowMainWindow());
        var exitItem = new ToolStripMenuItem("❌ Salir (Restaurar todo)", null, async (_, _) => await ExitApplicationCleanlyAsync());

        menu.Items.AddRange([_trayToggleItem, purgeItem, new ToolStripSeparator(), openItem, exitItem]);

        _trayIcon = new NotifyIcon
        {
            Icon = Icon,
            Text = "GameGuard (Modo Normal)",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void LoadConfigIntoUi()
    {
        _chkSteamChildren.Checked = _config.AutoAuthorizeSteamChildren;
        _chkDeepRam.Checked = _config.DeepRamPurge;
        _chkPowerPlan.Checked = _config.SwitchPowerPlan;
        _chkSteamLite.Checked = _config.SteamLiteMode;
        _chkStrictWhitelist.Checked = _config.StrictUserWhitelist;

        RefreshListBoxes();
        PopulateRunningProcessesCombo();
    }

    private void SaveUiToConfig()
    {
        _config.AutoAuthorizeSteamChildren = _chkSteamChildren.Checked;
        _config.BoostGameCpuPriority = _chkSteamChildren.Checked;
        _config.DeepRamPurge = _chkDeepRam.Checked;
        _config.SwitchPowerPlan = _chkPowerPlan.Checked;
        _config.SteamLiteMode = _chkSteamLite.Checked;
        _config.StrictUserWhitelist = _chkStrictWhitelist.Checked;
        _config.Save();
    }

    private void RefreshListBoxes()
    {
        _lstWhitelist.Items.Clear();
        foreach (var item in _config.WhitelistedProcesses.OrderBy(x => x))
            _lstWhitelist.Items.Add(item);

        _lstBlacklist.Items.Clear();
        foreach (var item in _config.ExplicitBlockedProcesses.OrderBy(x => x))
            _lstBlacklist.Items.Add(item);
    }

    private void PopulateRunningProcessesCombo()
    {
        _cmbRunningProcs.Items.Clear();
        var names = Process.GetProcesses()
            .Select(p =>
            {
                try { return ProcessGuardian.NormalizeName(p.ProcessName); }
                finally { p.Dispose(); }
            })
            .Where(n =>
                !string.IsNullOrEmpty(n) &&
                !GameGuardConfig.KernelSafeList.Contains(n) &&
                !_config.WhitelistedProcesses.Contains(n, StringComparer.OrdinalIgnoreCase) &&
                !_config.ExplicitBlockedProcesses.Contains(n, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToArray();

        _cmbRunningProcs.Items.AddRange(names);
    }

    private void BrowseForExecutable()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Seleccionar archivo ejecutable (.exe)",
            Filter = "Ejecutables (*.exe)|*.exe|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.FileName))
        {
            string exeName = Path.GetFileNameWithoutExtension(dlg.FileName);
            _txtNewProcess.Text = ProcessGuardian.NormalizeName(exeName);
        }
    }

    private void AddProcessToList(bool isWhitelist)
    {
        string name = ProcessGuardian.NormalizeName(_txtNewProcess.Text);
        if (string.IsNullOrEmpty(name))
            return;

        if (GameGuardConfig.KernelSafeList.Contains(name))
        {
            MessageBox.Show($"'{name}' ya es un proceso protegido del sistema Windows.", "Proceso del Sistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (isWhitelist)
        {
            _config.ExplicitBlockedProcesses.RemoveAll(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!_config.WhitelistedProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
                _config.WhitelistedProcesses.Add(name);
            AppendLog($"✅ '{name}' añadido a la Lista Blanca.");
        }
        else
        {
            _config.WhitelistedProcesses.RemoveAll(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!_config.ExplicitBlockedProcesses.Contains(name, StringComparer.OrdinalIgnoreCase))
                _config.ExplicitBlockedProcesses.Add(name);
            AppendLog($"🚫 '{name}' añadido a la Lista Negra explícita.");
        }

        _config.Save();
        RefreshListBoxes();
        PopulateRunningProcessesCombo();
        _txtNewProcess.Clear();
    }

    public async Task ToggleGameModeAsync()
    {
        if (_isBusy)
            return;

        _isBusy = true;
        _btnToggleGameMode.Enabled = false;

        try
        {
            if (!_isGameModeActive)
            {
                await ActivateGameModeAsync();
            }
            else
            {
                await DeactivateGameModeAsync();
            }
        }
        finally
        {
            _btnToggleGameMode.Enabled = true;
            _isBusy = false;
            UpdateTelemetryUi();
        }
    }

    private async Task ActivateGameModeAsync()
    {
        AppendLog("==================================================================");
        AppendLog("🚀 ACTIVANDO MODO JUEGO...");
        var memBefore = MemoryOptimizer.GetMemorySnapshot();

        // 1. Switch Power Plan to Ultimate Performance
        if (_config.SwitchPowerPlan)
        {
            bool powerOk = _powerPlanManager.ActivateGamePowerPlan(_config.GamePowerPlanGuid);
            AppendLog(powerOk
                ? "⚡ Plan de energía cambiado a: Ultimate Performance."
                : "ℹ️ Plan de energía mantenido.");
        }

        // 2. Pause non-essential background services
        AppendLog("⏸️ Pausando servicios no esenciales en segundo plano (Telemetría, Search, Updates, Office)...");
        var pausedResults = await _serviceManager.PauseServicesAsync(_config.ServicesToPause, AppendLogThreadSafe);
        int pausedCount = pausedResults.Count(r => r.Success);
        AppendLog($"✔️ Servicios pausados temporalmente: {pausedCount}.");

        // 3. Initial process purge
        AppendLog("🛑 Ejecutando barrido de procesos fuera de Lista Blanca...");
        var (killedCount, killedFreedMB) = await _guardian.ExecuteInitialPurgeAsync();
        AppendLog($"✔️ Procesos cerrados en barrido inicial: {killedCount} (~{killedFreedMB} MB).");

        // 4. Steam Lite Mode
        if (_config.SteamLiteMode)
        {
            var (steamSavedMB, helperCount) = MemoryOptimizer.ApplySteamLiteMode();
            if (helperCount > 0)
            {
                AppendLog($"🎮 Modo Steam Ligero aplicado a {helperCount} procesos steamwebhelper (liberados ~{steamSavedMB} MB, prioridad reducida).");
            }
        }

        // 5. Deep RAM Purge (Working Sets + Standby List, excluding active game PIDs!)
        if (_config.DeepRamPurge)
        {
            var gamePids = new HashSet<int>(_guardian.AuthorizedGames.Keys);
            var (_, trimmedCount) = await Task.Run(() => MemoryOptimizer.PurgeRam(gamePids));
            AppendLog($"🧹 Purgado profundo de RAM completado ({trimmedCount} Working Sets vaciados + Standby Cache, memoria del juego protegida).");
        }

        // 6. Start Real-Time Guardian Watcher
        _guardian.StartRealTimeGuardian();

        var memAfter = MemoryOptimizer.GetMemorySnapshot();
        double totalGained = Math.Max(0, Math.Round(memAfter.AvailableMB - memBefore.AvailableMB, 1));
        _sessionRamFreedMB += totalGained;

        _isGameModeActive = true;
        _lblStatusBadge.Text = "● MODO JUEGO ACTIVO";
        _lblStatusBadge.ForeColor = NeonGreen;
        _btnToggleGameMode.Text = "⏹️ DESACTIVAR MODO JUEGO Y RESTAURAR SISTEMA";
        _btnToggleGameMode.BackColor = Color.FromArgb(16, 185, 129);
        _trayToggleItem.Text = "⏹️ Desactivar Modo Juego";
        Icon = CreateDynamicIcon(NeonGreen);
        _trayIcon.Icon = Icon;
        _trayIcon.Text = $"GameGuard — ACTIVO (+{_sessionRamFreedMB} MB libres)";

        AppendLog($"✅ MODO JUEGO ACTIVO. RAM Libre actual: {memAfter.AvailableMB:N0} MB (Ganancia neta: +{totalGained:N1} MB).");
        AppendLog("==================================================================");
    }

    private async Task DeactivateGameModeAsync()
    {
        AppendLog("==================================================================");
        AppendLog("🔄 DESACTIVANDO MODO JUEGO Y RESTAURANDO SISTEMA...");

        // 1. Stop Real-Time Guardian
        _guardian.StopRealTimeGuardian();

        // 2. Restore Steam WebHelper priority
        if (_config.SteamLiteMode)
        {
            MemoryOptimizer.RestoreSteamNormalMode();
            AppendLog("▶️ Prioridad de procesos Steam restaurada a Normal.");
        }

        // 3. Restore paused Windows services
        int restoredServices = await _serviceManager.RestoreServicesAsync(AppendLogThreadSafe);
        AppendLog($"▶️ Servicios de Windows restaurados: {restoredServices}.");

        // 4. Restore Power Plan
        if (_config.SwitchPowerPlan)
        {
            _powerPlanManager.RestorePowerPlan(_config.BalancedPowerPlanGuid);
            var activePlan = PowerPlanManager.GetActivePlan();
            AppendLog($"⚡ Plan de energía actual: {activePlan.Name}.");
        }

        _isGameModeActive = false;
        _lblStatusBadge.Text = "● MODO NORMAL";
        _lblStatusBadge.ForeColor = TextMuted;
        _btnToggleGameMode.Text = "🚀 ACTIVAR MODO JUEGO (PURGAR Y VIGILAR)";
        _btnToggleGameMode.BackColor = OmenRed;
        _trayToggleItem.Text = "🚀 Activar Modo Juego";
        Icon = CreateDynamicIcon(OmenRed);
        _trayIcon.Icon = Icon;
        _trayIcon.Text = "GameGuard (Modo Normal)";

        AppendLog("✅ Sistema restaurado a Modo Normal.");
        AppendLog("==================================================================");
    }

    private void ExecuteManualRamPurge()
    {
        _guardian.RefreshLauncherAndGameTree();
        var gamePids = new HashSet<int>(_guardian.AuthorizedGames.Keys);
        var (freed, trimmed) = MemoryOptimizer.PurgeRam(gamePids);
        _sessionRamFreedMB += freed;
        AppendLog($"🧹 Purgado manual de RAM ejecutado: +{freed:N1} MB liberados ({trimmed} procesos optimizados, juego protegido).");
        UpdateTelemetryUi();
    }

    private void RunDryRunSimulation()
    {
        AppendLog("------------------------------------------------------------------");
        AppendLog("🔍 SIMULACIÓN (DRY RUN) — Qué se cerraría o pausaría ahora mismo:");

        int procCount = 0;
        double procMemMB = 0;

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

                    if (_guardian.EvaluateProcess(name, pid, parentPid, path, out string reason))
                    {
                        double mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
                        procCount++;
                        procMemMB += mb;
                        AppendLog($"   [Proceso a cerrar] {name}.exe (PID {pid}, {mb} MB) -> {reason}");
                    }
                }
                catch { }
            }
        }

        int svcCount = 0;
        foreach (string svcName in _config.ServicesToPause.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var sc = new ServiceController(svcName);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    svcCount++;
                    AppendLog($"   [Servicio a pausar] {sc.DisplayName} ({svcName})");
                }
            }
            catch { }
        }

        AppendLog($"📊 Resultado Simulación: Se cerrarían {procCount} procesos (~{procMemMB:N1} MB) y se pausarían {svcCount} servicios activos (+ ahorro de Modo Steam Ligero y Purgado de Caché RAM).");
        AppendLog("------------------------------------------------------------------");
    }

    private void UpdateTelemetryUi()
    {
        if (IsDisposed)
            return;

        var mem = MemoryOptimizer.GetMemorySnapshot();
        _lblCardRamValue.Text = $"{mem.AvailableMB:N0} MB Libres";
        _lblCardRamSub.Text = $"En uso: {mem.UsedPercent}% de {mem.TotalMB:N0} MB | Ganado: +{_sessionRamFreedMB:N0} MB";

        _lblCardProcsValue.Text = $"{_guardian.TotalKilledCount} Neutralizados";
        _lblCardProcsSub.Text = $"Bloqueados en vivo: {_guardian.TotalBlockedLiveCount} (~{_guardian.TotalMemoryFreedByKillsMB:N0} MB)";

        int pausedSvcs = _serviceManager.ServicesStoppedBySession.Count;
        _lblCardServicesValue.Text = $"{pausedSvcs} Pausados";
        _lblCardServicesSub.Text = _isGameModeActive
            ? "Guardián WMI vigilando aperturas"
            : $"Configurados para pausa: {_config.ServicesToPause.Count}";

        var games = _guardian.AuthorizedGames.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var plan = PowerPlanManager.GetActivePlan();
        _lblCardGameValue.Text = games.Count > 0
            ? $"🎮 {string.Join(", ", games)}"
            : (_isGameModeActive ? "Esperando juego..." : "Modo Normal");
        _lblCardGameSub.Text = $"Energía: {plan.Name}";
    }

    private void OnGuardianProcessTerminated(ProcessActionInfo info)
    {
        if (IsDisposed)
            return;
        BeginInvoke(UpdateTelemetryUi);
    }

    private void OnGuardianGameDetected(string gameName, int pid)
    {
        if (IsDisposed)
            return;
        BeginInvoke(() =>
        {
            UpdateTelemetryUi();
            _trayIcon.ShowBalloonTip(
                3000,
                "🎮 Juego Detectado por GameGuard",
                $"{gameName}.exe autorizado automáticamente con prioridad de CPU ALTA.",
                ToolTipIcon.Info);
        });
    }

    private void AppendLogThreadSafe(string message)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
        }
        else
        {
            AppendLog(message);
        }
    }

    private void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        _txtLog.SelectionStart = _txtLog.TextLength;
        _txtLog.SelectionLength = 0;

        if (message.Contains("🚫") || message.Contains("🛑"))
            _txtLog.SelectionColor = Color.FromArgb(248, 113, 113);
        else if (message.Contains("✅") || message.Contains("🎮"))
            _txtLog.SelectionColor = NeonGreen;
        else if (message.Contains("⚠️") || message.Contains("⏸️"))
            _txtLog.SelectionColor = Color.FromArgb(250, 204, 21);
        else if (message.Contains("🧹") || message.Contains("🔍"))
            _txtLog.SelectionColor = AccentBlue;
        else
            _txtLog.SelectionColor = Color.FromArgb(226, 232, 240);

        _txtLog.AppendText(line);
        _txtLog.ScrollToCaret();
    }

    private void RelaunchAsAdmin()
    {
        try
        {
            string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            var psi = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            _allowClose = true;
            Application.Exit();
        }
        catch
        {
            AppendLog("ℹ️ Elevación UAC cancelada por el usuario.");
        }
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing && _isGameModeActive)
        {
            // When Game Mode is active, closing the window minimizes it to System Tray so the Guardian keeps protecting the game!
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(
                2500,
                "GameGuard sigue activo",
                "El Guardián sigue protegiendo tu juego en la bandeja del sistema. Haz clic derecho en el icono para salir.",
                ToolTipIcon.Info);
            return;
        }

        base.OnFormClosing(e);
    }

    private async Task ExitApplicationCleanlyAsync()
    {
        _allowClose = true;
        if (_isGameModeActive)
        {
            await DeactivateGameModeAsync();
        }
        _uiTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _guardian.Dispose();
        Application.Exit();
    }

    private static Icon CreateDynamicIcon(Color accent)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Draw OMEN diamond shield
            Point[] diamond =
            [
                new Point(16, 2),
                new Point(30, 16),
                new Point(16, 30),
                new Point(2, 16)
            ];
            using var bgBrush = new SolidBrush(Color.FromArgb(20, 23, 31));
            using var borderPen = new Pen(accent, 2.5f);
            g.FillPolygon(bgBrush, diamond);
            g.DrawPolygon(borderPen, diamond);

            using var innerBrush = new SolidBrush(accent);
            Point[] inner =
            [
                new Point(16, 9),
                new Point(23, 16),
                new Point(16, 23),
                new Point(9, 16)
            ];
            g.FillPolygon(innerBrush, inner);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
