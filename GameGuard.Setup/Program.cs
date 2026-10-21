namespace GameGuard.Setup;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        bool isSilent = args.Contains("--silent", StringComparer.OrdinalIgnoreCase) ||
                        args.Contains("/S", StringComparer.OrdinalIgnoreCase);
        bool isUninstall = args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase);

        if (isSilent)
        {
            try
            {
                if (isUninstall)
                {
                    InstallerEngine.Uninstall();
                }
                else
                {
                    InstallerEngine.Install(new InstallOptions { LaunchAfterInstall = false });
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm(startInUninstallMode: isUninstall));
        return 0;
    }
}
