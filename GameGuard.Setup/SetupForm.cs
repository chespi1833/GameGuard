using System.Diagnostics;

namespace GameGuard.Setup;

public sealed class SetupForm : Form
{
    private readonly TextBox _txtInstallPath;
    private readonly CheckBox _chkPublicDesktop;
    private readonly CheckBox _chkStartMenu;
    private readonly CheckBox _chkSharedConfig;
    private readonly CheckBox _chkLaunchNow;
    private readonly ProgressBar _progressBar;
    private readonly Label _lblStatus;
    private readonly Button _btnInstall;
    private readonly Button _btnUninstall;

    private static readonly Color BgDark = Color.FromArgb(15, 17, 23);
    private static readonly Color BgCard = Color.FromArgb(25, 29, 38);
    private static readonly Color AccentRed = Color.FromArgb(230, 0, 46);
    private static readonly Color NeonGreen = Color.FromArgb(0, 214, 114);
    private static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    public SetupForm(bool startInUninstallMode = false)
    {
        Text = "Instalador de GameGuard — Todos los Usuarios";
        Size = new Size(620, 470);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgDark;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f);

        // Header
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Color.FromArgb(20, 23, 31)
        };

        var lblTitle = new Label
        {
            Text = "INSTALADOR DE GAMEGUARD",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = AccentRed,
            AutoSize = true,
            Location = new Point(22, 14)
        };

        var lblSub = new Label
        {
            Text = "Instalación global en esta máquina para todos los usuarios (All-Users)",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(24, 42)
        };
        header.Controls.AddRange([lblTitle, lblSub]);

        // Content Card
        var card = new Panel
        {
            Location = new Point(22, 90),
            Size = new Size(560, 240),
            BackColor = BgCard
        };

        var lblPath = new Label
        {
            Text = "Carpeta de instalación (Program Files):",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(18, 16)
        };

        _txtInstallPath = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GameGuard"),
            Location = new Point(18, 40),
            Size = new Size(524, 28),
            BackColor = Color.FromArgb(34, 39, 51),
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        _chkPublicDesktop = new CheckBox
        {
            Text = "Crear acceso directo en el Escritorio Público (visible para todos los usuarios)",
            Checked = true,
            AutoSize = true,
            Location = new Point(18, 82),
            ForeColor = TextPrimary
        };

        _chkStartMenu = new CheckBox
        {
            Text = "Crear acceso directo en el Menú Inicio de todos los usuarios",
            Checked = true,
            AutoSize = true,
            Location = new Point(18, 114),
            ForeColor = TextPrimary
        };

        _chkSharedConfig = new CheckBox
        {
            Text = "Configurar perfil compartido en C:\\ProgramData\\GameGuard (editable por todos)",
            Checked = true,
            AutoSize = true,
            Location = new Point(18, 146),
            ForeColor = TextPrimary
        };

        _chkLaunchNow = new CheckBox
        {
            Text = "Iniciar GameGuard al terminar la instalación",
            Checked = true,
            AutoSize = true,
            Location = new Point(18, 182),
            ForeColor = NeonGreen,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };

        card.Controls.AddRange([lblPath, _txtInstallPath, _chkPublicDesktop, _chkStartMenu, _chkSharedConfig, _chkLaunchNow]);

        // Progress & Status
        _lblStatus = new Label
        {
            Text = "Listo para instalar en el equipo.",
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(22, 342)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(22, 366),
            Size = new Size(560, 12),
            Style = ProgressBarStyle.Continuous
        };

        _btnInstall = new Button
        {
            Text = "🚀 INSTALAR PARA TODOS LOS USUARIOS",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            BackColor = AccentRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(365, 42),
            Location = new Point(22, 388),
            Cursor = Cursors.Hand
        };
        _btnInstall.FlatAppearance.BorderSize = 0;
        _btnInstall.Click += async (_, _) => await RunInstallAsync();

        _btnUninstall = new Button
        {
            Text = "🗑️ Desinstalar",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 42),
            Location = new Point(402, 388),
            Cursor = Cursors.Hand
        };
        _btnUninstall.FlatAppearance.BorderSize = 0;
        _btnUninstall.Click += async (_, _) => await RunUninstallAsync();

        Controls.AddRange([header, card, _lblStatus, _progressBar, _btnInstall, _btnUninstall]);

        if (startInUninstallMode)
        {
            Shown += async (_, _) => await RunUninstallAsync();
        }
    }

    private async Task RunInstallAsync()
    {
        _btnInstall.Enabled = false;
        _btnUninstall.Enabled = false;

        var options = new InstallOptions
        {
            InstallDir = _txtInstallPath.Text.Trim(),
            CreatePublicDesktopShortcut = _chkPublicDesktop.Checked,
            CreateAllUsersStartMenuShortcut = _chkStartMenu.Checked,
            ConfigureSharedProgramData = _chkSharedConfig.Checked,
            RegisterInWindowsUninstall = true,
            LaunchAfterInstall = _chkLaunchNow.Checked
        };

        try
        {
            await Task.Run(() =>
            {
                InstallerEngine.Install(options, (msg, pct) =>
                {
                    BeginInvoke(() =>
                    {
                        _lblStatus.Text = msg;
                        _progressBar.Value = Math.Clamp(pct, 0, 100);
                    });
                });
            });

            _lblStatus.ForeColor = NeonGreen;
            string installedExe = Path.Combine(options.InstallDir, "GameGuard.exe");

            if (options.LaunchAfterInstall && File.Exists(installedExe))
            {
                Process.Start(new ProcessStartInfo(installedExe)
                {
                    WorkingDirectory = options.InstallDir,
                    UseShellExecute = true
                });
            }

            MessageBox.Show(
                $"GameGuard se ha instalado correctamente para todos los usuarios en:\n\n{options.InstallDir}\n\n" +
                "• Acceso directo creado en el Escritorio Público (C:\\Users\\Public\\Desktop)\n" +
                "• Acceso directo creado en el Menú Inicio global\n" +
                "• Configuración compartida en C:\\ProgramData\\GameGuard",
                "Instalación Completada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.Salmon;
            _lblStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error durante la instalación:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnInstall.Enabled = true;
            _btnUninstall.Enabled = true;
        }
    }

    private async Task RunUninstallAsync()
    {
        var confirm = MessageBox.Show(
            "¿Deseas desinstalar GameGuard para todos los usuarios de este equipo?",
            "Confirmar Desinstalación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        _btnInstall.Enabled = false;
        _btnUninstall.Enabled = false;

        try
        {
            string dir = _txtInstallPath.Text.Trim();
            await Task.Run(() =>
            {
                InstallerEngine.Uninstall(dir, (msg, pct) =>
                {
                    BeginInvoke(() =>
                    {
                        _lblStatus.Text = msg;
                        _progressBar.Value = Math.Clamp(pct, 0, 100);
                    });
                });
            });

            MessageBox.Show("GameGuard fue desinstalado correctamente del equipo.", "Desinstalado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al desinstalar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnInstall.Enabled = true;
            _btnUninstall.Enabled = true;
        }
    }
}
